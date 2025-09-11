using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a field as requiring validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxValidationAttribute : PropertyAttribute
    {
        /// <summary>
        /// Type of validator to use
        /// </summary>
        public Type ValidatorType { get; }

        /// <summary>
        /// Custom validation message
        /// </summary>
        public string ValidationMessage { get; set; }

        /// <summary>
        /// Whether to validate on every change
        /// </summary>
        public bool ValidateOnChange { get; set; } = true;

        /// <summary>
        /// Whether to show validation in the editor
        /// </summary>
        public bool ShowInEditor { get; set; } = true;

        public FluxValidationAttribute(Type validatorType)
        {
            ValidatorType = validatorType;
        }
    }

    /// <summary>
    /// Built-in range validation attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxRangeAttribute : FluxValidationAttribute
    {
        /// <summary>
        /// Minimum value
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// Maximum value
        /// </summary>
        public float Max { get; }

        /// <summary>
        /// Whether the range is inclusive
        /// </summary>
        public bool Inclusive { get; set; } = true;

        public FluxRangeAttribute(float min, float max) : base(null)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Built-in string length validation attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxStringLengthAttribute : FluxValidationAttribute
    {
        /// <summary>
        /// Minimum string length
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Maximum string length
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Whether empty strings are allowed
        /// </summary>
        public bool AllowEmpty { get; set; } = true;

        public FluxStringLengthAttribute(int minLength, int maxLength) : base(null)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }
    }
}
