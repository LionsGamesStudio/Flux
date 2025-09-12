using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Validation;
using FluxFramework.Binding;
using FluxFramework.Attributes;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// Extension methods for ReactiveProperty to enable LINQ-style reactive operations.
    /// </summary>
    public static class ReactivePropertyExtensions
    {
        /// <summary>
        /// Creates a reactive property that enforces a validation rule.
        /// </summary>
        public static ValidatedReactiveProperty<T> WithValidation<T>(T initialValue, Func<T, bool> validatorFunc)
        {
            if (validatorFunc == null) throw new ArgumentNullException(nameof(validatorFunc));
            
            var wrapperValidator = new FuncValidator<T>(validatorFunc);
            var validators = new List<IValidator<T>> { wrapperValidator };
            return new ValidatedReactiveProperty<T>(initialValue, validators);
        }
        
        /// <summary>
        /// Creates a new reactive property by applying a non-generic IValueConverter.
        /// This is the core of the automatic conversion in the binding system.
        /// </summary>
        public static ReactiveProperty<TTarget> Transform<TTarget>(
            this IReactiveProperty source, 
            IValueConverter converter)
        {
            TTarget initialValue;
            try
            {
                initialValue = (TTarget)converter.Convert(source.GetValue());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] ValueConverter '{converter.GetType().Name}' failed during initial conversion: {ex.Message}");
                initialValue = default;
            }

            var target = new ReactiveProperty<TTarget>(initialValue);

            // Subscribe to the source property.
            var subscription = source.Subscribe(value =>
            {
                try
                {
                    target.Value = (TTarget)converter.Convert(value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] ValueConverter '{converter.GetType().Name}' failed during update: {ex.Message}");
                }
            });

            // This requires a small addition to the ReactiveProperty class.
            target.AddDependentSubscription(subscription);
            
            return target;
        }

        /// <summary>
        /// Creates a new reactive property by applying a non-generic IValueConverter.
        /// This is the core of the automatic conversion in the binding system.
        /// This version now supports TwoWay binding.
        /// </summary>
        public static ReactiveProperty<TTarget> Transform<TTarget>(
            this IReactiveProperty source, 
            IValueConverter converter,
            BindingOptions options) // <-- Pass options to know the binding mode
        {
            TTarget initialValue;
            try { initialValue = (TTarget)converter.Convert(source.GetValue()); }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] ValueConverter '{converter.GetType().Name}' failed during initial conversion: {ex.Message}");
                initialValue = default;
            }

            var target = new ReactiveProperty<TTarget>(initialValue);
            bool isUpdating = false; // Re-entrancy guard to prevent infinite loops

            // --- FORWARD BINDING (Source -> Target) ---
            var sub1 = source.Subscribe(value =>
            {
                if (isUpdating) return;
                try
                {
                    isUpdating = true;
                    target.Value = (TTarget)converter.Convert(value);
                }
                catch (Exception ex) { Debug.LogError($"[FluxFramework] ValueConverter '{converter.GetType().Name}' failed during update: {ex.Message}"); }
                finally { isUpdating = false; }
            });
            target.AddDependentSubscription(sub1);

            // --- BACKWARD BINDING (Target -> Source) for TwoWay mode ---
            if (options.Mode == BindingMode.TwoWay)
            {
                var sub2 = target.Subscribe((TTarget value) =>
                {
                    if (isUpdating) return;
                    try
                    {
                        isUpdating = true;
                        source.SetValue(converter.ConvertBack(value));
                    }
                    catch (Exception ex) { Debug.LogError($"[FluxFramework] ValueConverter '{converter.GetType().Name}' failed during ConvertBack: {ex.Message}"); }
                    finally { isUpdating = false; }
                });
                target.AddDependentSubscription(sub2);
            }
            
            return target;
        }

        /// <summary>
        /// Creates a new reactive property whose value is a transformation of the source property.
        /// </summary>
        public static ReactiveProperty<TTarget> Transform<TSource, TTarget>(
            this ReactiveProperty<TSource> source,
            Func<TSource, TTarget> transform)
        {
            var target = new ReactiveProperty<TTarget>(transform(source.Value));
            var subscription = source.Subscribe(value => target.Value = transform(value));
            target.AddDependentSubscription(subscription);
            return target;
        }

        /// <summary>
        /// Combines two reactive properties into a single new property.
        /// </summary>
        public static ReactiveProperty<TResult> CombineWith<T1, T2, TResult>(
            this ReactiveProperty<T1> prop1,
            ReactiveProperty<T2> prop2,
            Func<T1, T2, TResult> combiner)
        {
            var result = new ReactiveProperty<TResult>(combiner(prop1.Value, prop2.Value));
            
            var sub1 = prop1.Subscribe(value => result.Value = combiner(value, prop2.Value));
            var sub2 = prop2.Subscribe(value => result.Value = combiner(prop1.Value, value));

            result.AddDependentSubscription(sub1);
            result.AddDependentSubscription(sub2);
            
            return result;
        }

        /// <summary>
        /// Creates a new reactive property that only updates when the source value passes a filter.
        /// </summary>
        public static ReactiveProperty<T> Where<T>(this ReactiveProperty<T> source, Func<T, bool> filter)
        {
            var filtered = new ReactiveProperty<T>(source.Value);
            var subscription = source.Subscribe(value =>
            {
                if (filter(value))
                {
                    filtered.Value = value;
                }
            });
            filtered.AddDependentSubscription(subscription);
            return filtered;
        }

        /// <summary>
        /// A private helper class that wraps a simple Func<T, bool> into the IValidator<T> interface.
        /// </summary>
        private class FuncValidator<T> : IValidator<T>
        {
            private readonly Func<T, bool> _validationFunc;
            public FuncValidator(Func<T, bool> validationFunc) { _validationFunc = validationFunc; }
            public ValidationResult Validate(T value)
            {
                return _validationFunc(value) ? ValidationResult.Success : ValidationResult.Failure("Value did not pass the validation function.");
            }
        }
    }
}