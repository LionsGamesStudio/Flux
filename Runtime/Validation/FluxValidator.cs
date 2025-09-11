using System;
using System.Reflection;
using FluxFramework.Attributes;
using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// A static utility class that provides methods to validate fields and values
    /// based on Flux validation attributes.
    /// </summary>
    public static class FluxValidator
    {
        /// <summary>
        /// Validates the current value of a field against all its Flux validation attributes.
        /// </summary>
        /// <param name="field">The FieldInfo of the field to validate.</param>
        /// <param name="owner">The object instance that owns the field.</param>
        /// <param name="errorMessage">The detailed error message if validation fails.</param>
        /// <returns>True if the field's value is valid, otherwise false.</returns>
        public static bool ValidateField(FieldInfo field, object owner, out string errorMessage)
        {
            var value = field.GetValue(owner);
            return ValidateValue(field, value, out errorMessage);
        }

        /// <summary>
        /// Validates a given value against the Flux validation attributes of a specific field.
        /// </summary>
        /// <param name="field">The FieldInfo that contains the validation attributes.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="errorMessage">The detailed error message if validation fails.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        public static bool ValidateValue(MemberInfo field, object value, out string errorMessage)
        {
            errorMessage = "";

            // --- Range Validation ---
            var rangeAttr = field.GetCustomAttribute<FluxRangeAttribute>();
            if (rangeAttr != null)
            {
                if (value is int intValue)
                {
                    if (intValue < rangeAttr.Min || intValue > rangeAttr.Max)
                    {
                        errorMessage = $"Value {intValue} is out of the allowed range [{rangeAttr.Min}, {rangeAttr.Max}].";
                        return false;
                    }
                }
                else if (value is float floatValue)
                {
                    if (floatValue < rangeAttr.Min || floatValue > rangeAttr.Max)
                    {
                        errorMessage = $"Value {floatValue} is out of the allowed range [{rangeAttr.Min}, {rangeAttr.Max}].";
                        return false;
                    }
                }
            }

            // --- String Length Validation ---
            var stringLengthAttr = field.GetCustomAttribute<FluxStringLengthAttribute>();
            if (stringLengthAttr != null && value is string stringValue)
            {
                if (stringValue == null)
                {
                    stringValue = ""; // Treat null as empty for validation purposes
                }
                
                if (!stringLengthAttr.AllowEmpty && string.IsNullOrEmpty(stringValue))
                {
                    errorMessage = "The string cannot be null or empty.";
                    return false;
                }
                
                if (stringValue.Length < stringLengthAttr.MinLength || stringValue.Length > stringLengthAttr.MaxLength)
                {
                    errorMessage = $"String length {stringValue.Length} is outside the allowed range [{stringLengthAttr.MinLength}, {stringLengthAttr.MaxLength}].";
                    return false;
                }
            }
            
            // --- Custom Validator (via FluxValidationAttribute) ---
            // This part makes the system extensible
            var customValidationAttr = field.GetCustomAttribute<FluxValidationAttribute>();
            if (customValidationAttr?.ValidatorType != null)
            {
                try
                {
                    // Check if the validator implements the correct generic interface
                    var validatorInterface = typeof(IValidator<>).MakeGenericType(field.GetType());
                    if (validatorInterface.IsAssignableFrom(customValidationAttr.ValidatorType))
                    {
                        var validatorInstance = Activator.CreateInstance(customValidationAttr.ValidatorType);
                        var validateMethod = validatorInstance.GetType().GetMethod("Validate");
                        
                        var result = (ValidationResult)validateMethod.Invoke(validatorInstance, new[] { value });

                        if (!result.IsValid)
                        {
                            errorMessage = string.Join(", ", result.ErrorMessages);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Failed to execute custom validator '{customValidationAttr.ValidatorType.Name}': {ex.Message}");
                    errorMessage = "An error occurred during custom validation.";
                    return false;
                }
            }

            return true; // All validations passed
        }
    }
}