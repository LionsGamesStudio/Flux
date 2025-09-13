using System;
using FluxFramework.Core;

namespace FluxFramework.Validation
{
    /// <summary>
    /// A "live" validator that checks if a value is within a range where the min and/or max
    /// bounds are themselves determined by other ReactiveProperties. This validator maintains
    /// subscriptions to the boundary properties and disposes of them when it is cleaned up.
    /// </summary>
    public class DynamicRangeValidator<T> : IValidator<T>, IDisposable where T : struct, IComparable
    {
        private T? _currentMin;
        private T? _currentMax;
        
        private IDisposable _minSubscription;
        private IDisposable _maxSubscription;

        public DynamicRangeValidator(T? staticMin, string minKey, T? staticMax, string maxKey)
        {
            _currentMin = staticMin;
            _currentMax = staticMax;

            // Subscribe to the min property if a key was provided.
            if (!string.IsNullOrEmpty(minKey))
            {
                _minSubscription = Flux.Manager.Properties.SubscribeDeferred(minKey, prop =>
                {
                    if (prop is ReactiveProperty<T> typedProp)
                    {
                        // Once the property is found, create a live subscription to its changes.
                        // fireOnSubscribe:true ensures we get the initial value immediately.
                        _minSubscription = typedProp.Subscribe(newMin => _currentMin = newMin, fireOnSubscribe: true);
                    }
                    else if(prop != null)
                    {
                         UnityEngine.Debug.LogWarning($"[FluxFramework] DynamicRangeValidator: Mismatch between property type of '{minKey}' ({prop.ValueType.Name}) and validated field type ({typeof(T).Name}).");
                    }
                });
            }

            // Subscribe to the max property if a key was provided.
            if (!string.IsNullOrEmpty(maxKey))
            {
                _maxSubscription = Flux.Manager.Properties.SubscribeDeferred(maxKey, prop =>
                {
                    if (prop is ReactiveProperty<T> typedProp)
                    {
                        _maxSubscription = typedProp.Subscribe(newMax => _currentMax = newMax, fireOnSubscribe: true);
                    }
                    else if(prop != null)
                    {
                        UnityEngine.Debug.LogWarning($"[FluxFramework] DynamicRangeValidator: Mismatch between property type of '{maxKey}' ({prop.ValueType.Name}) and validated field type ({typeof(T).Name}).");
                    }
                });
            }
        }

        /// <summary>
        /// Validates the given value against the current min and max bounds.
        /// </summary>
        public ValidationResult Validate(T value)
        {
            bool isMinValid = !_currentMin.HasValue || value.CompareTo(_currentMin.Value) >= 0;
            bool isMaxValid = !_currentMax.HasValue || value.CompareTo(_currentMax.Value) <= 0;

            if (isMinValid && isMaxValid)
            {
                return ValidationResult.Success;
            }

            string minText = _currentMin.HasValue ? _currentMin.Value.ToString() : "-∞";
            string maxText = _currentMax.HasValue ? _currentMax.Value.ToString() : "+∞";
            
            return ValidationResult.Failure($"Value {value} is outside the dynamic range [{minText}, {maxText}].");
        }

        /// <summary>
        /// Cleans up the subscriptions to prevent memory leaks.
        /// This is crucial for a stateful validator.
        /// </summary>
        public void Dispose()
        {
            _minSubscription?.Dispose();
            _maxSubscription?.Dispose();
        }
    }
}