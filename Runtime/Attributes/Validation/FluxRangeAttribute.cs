using System;
using System.Reflection;
using FluxFramework.Validation;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// An advanced validation attribute that ensures a numeric value stays within a specified range.
    /// The range bounds can be either static constants or dynamically linked to other ReactiveProperties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxRangeAttribute : FluxValidationAttribute
    {
        // --- INTERNAL STATE ---
        private readonly float? _staticMin;
        private readonly float? _staticMax;
        private readonly string _minPropertyKey;
        private readonly string _maxPropertyKey;

        // --- CONSTRUCTORS ---

        /// <summary>
        /// Validates against a static, constant range.
        /// </summary>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        public FluxRangeAttribute(float min, float max)
        {
            _staticMin = min;
            _staticMax = max;
        }

        /// <summary>
        /// Validates against a range with a static minimum and a dynamic maximum bound to a ReactiveProperty.
        /// </summary>
        /// <param name="min">The static minimum allowed value.</param>
        /// <param name="maxPropertyKey">The key of the ReactiveProperty that defines the maximum value.</param>
        public FluxRangeAttribute(float min, string maxPropertyKey)
        {
            _staticMin = min;
            _maxPropertyKey = maxPropertyKey;
        }

        /// <summary>
        /// Validates against a range with a dynamic minimum bound to a ReactiveProperty and a static maximum.
        /// </summary>
        /// <param name="minPropertyKey">The key of the ReactiveProperty that defines the minimum value.</param>
        /// <param name="max">The static maximum allowed value.</param>
        public FluxRangeAttribute(string minPropertyKey, float max)
        {
            _minPropertyKey = minPropertyKey;
            _staticMax = max;
        }

        /// <summary>
        /// Validates against a fully dynamic range where both bounds are defined by other ReactiveProperties.
        /// </summary>
        /// <param name="minPropertyKey">The key of the ReactiveProperty that defines the minimum value.</param>
        /// <param name="maxPropertyKey">The key of the ReactiveProperty that defines the maximum value.</param>
        public FluxRangeAttribute(string minPropertyKey, string maxPropertyKey)
        {
            _minPropertyKey = minPropertyKey;
            _maxPropertyKey = maxPropertyKey;
        }

        /// <summary>
        /// A factory method that creates the appropriate validator (static or dynamic)
        /// based on the constructor that was used.
        /// </summary>
        public override IValidator CreateValidator(FieldInfo field)
        {
            Type valueType = GetValueTypeFromField(field);

            if (!typeof(IComparable).IsAssignableFrom(valueType))
            {
                Debug.LogWarning($"[FluxFramework] FluxRangeAttribute cannot be applied to field '{field.Name}' because its type '{valueType.Name}' is not comparable.");
                return null;
            }

            // If no property keys were provided, use the simple, high-performance static validator.
            if (string.IsNullOrEmpty(_minPropertyKey) && string.IsNullOrEmpty(_maxPropertyKey))
            {
                var validatorType = typeof(RangeValidator<>).MakeGenericType(valueType);
                var min = ConvertValue(_staticMin.Value, valueType);
                var max = ConvertValue(_staticMax.Value, valueType);
                return (IValidator)Activator.CreateInstance(validatorType, min, max);
            }
            else // Otherwise, create the new "live" dynamic validator.
            {
                var validatorType = typeof(DynamicRangeValidator<>).MakeGenericType(valueType);
                var min = _staticMin.HasValue ? ConvertValue(_staticMin.Value, valueType) : null;
                var max = _staticMax.HasValue ? ConvertValue(_staticMax.Value, valueType) : null;

                return (IValidator)Activator.CreateInstance(validatorType, min, _minPropertyKey, max, _maxPropertyKey);
            }
        }

        #region Utility Methods
        
        /// <summary>
        /// Determines the underlying value type of a field, whether it's a primitive or a ReactiveProperty<T>.
        /// </summary>
        private Type GetValueTypeFromField(FieldInfo field)
        {
            if (typeof(IReactiveProperty).IsAssignableFrom(field.FieldType))
            {
                // It's an explicit ReactiveProperty<T>, so get T.
                return field.FieldType.GetGenericArguments()[0];
            }
            else
            {
                // It's an implicit property (e.g., int, float), so the field type is the value type.
                return field.FieldType;
            }
        }

        /// <summary>
        /// Safely converts a float value from the attribute to the target field's actual type.
        /// </summary>
        private object ConvertValue(float value, Type targetType)
        {
            if (IsIntegralType(targetType))
            {
                return Convert.ChangeType(Mathf.Round(value), targetType);
            }
            return Convert.ChangeType(value, targetType);
        }

        private static bool IsIntegralType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(uint) || type == typeof(ulong) || type == typeof(sbyte) || type == typeof(ushort);
        }

        #endregion
    }
}