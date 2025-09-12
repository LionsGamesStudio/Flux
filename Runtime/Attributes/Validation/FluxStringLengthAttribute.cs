using System;
using System.Reflection;
using FluxFramework.Validation;
using FluxFramework.Core;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Built-in string length validation attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxStringLengthAttribute : FluxValidationAttribute
    {
        public int MinLength { get; }
        public int MaxLength { get; }
        public bool AllowEmpty { get; set; } = true;

        public FluxStringLengthAttribute(int minLength, int maxLength)
        {
            MinLength = minLength;
            MaxLength = maxLength;
        }

        /// <summary>
        /// Implementation of the contract: Creates a StringLengthValidator.
        /// </summary>
        public override IValidator CreateValidator(FieldInfo field)
        {
            // The logic here is simple: it just needs to create the corresponding validator.
            // We pass 'this' (the attribute instance) to the validator's constructor
            // so it can retrieve MinLength, MaxLength, etc.
            return new StringLengthValidator(this);
        }
    }
}