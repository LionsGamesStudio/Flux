using System;
using System.Reflection;
using System.Globalization;
using FluxFramework.Validation;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Built-in range validation attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxRangeAttribute : FluxValidationAttribute
    {
        public float Min { get; }
        public float Max { get; }
        public bool Inclusive { get; set; } = true;

        public FluxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Implementation of the contract: Creates a generic RangeValidator instance
        /// with safely converted min/max values.
        /// </summary>
        public override IValidator CreateValidator(FieldInfo field)
        {
            // Determine the value type based on which pattern is being used
            Type valueType;
            if (typeof(IReactiveProperty).IsAssignableFrom(field.FieldType))
            {
                // Pattern B: Field is ReactiveProperty<T>, get T
                valueType = field.FieldType.GetGenericArguments()[0];
            }
            else
            {
                // Pattern A: Field is the type itself (int, float...)
                valueType = field.FieldType;
            }

            if (!typeof(IComparable).IsAssignableFrom(valueType))
            {
                Debug.LogWarning($"[FluxFramework] FluxRangeAttribute cannot be applied to field '{field.Name}' because its type '{valueType.Name}' is not comparable.");
                return null;
            }

            try
            {
                var validatorType = typeof(RangeValidator<>).MakeGenericType(valueType);
                object minConverted, maxConverted;

                // Safe conversion logic to handle floats vs. integers
                if (IsIntegralType(valueType))
                {
                    minConverted = Convert.ChangeType(Mathf.Round(Min), valueType, CultureInfo.InvariantCulture);
                    maxConverted = Convert.ChangeType(Mathf.Round(Max), valueType, CultureInfo.InvariantCulture);
                }
                else
                {
                    minConverted = Convert.ChangeType(Min, valueType, CultureInfo.InvariantCulture);
                    maxConverted = Convert.ChangeType(Max, valueType, CultureInfo.InvariantCulture);
                }

                return (IValidator)Activator.CreateInstance(validatorType, minConverted, maxConverted);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Failed to create RangeValidator for field '{field.Name}'. Error: {ex.Message}");
                return null;
            }
        }

        private static bool IsIntegralType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                   type == typeof(uint) || type == typeof(ulong) || type == typeof(sbyte) || type == typeof(ushort);
        }
    }
}