using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Validation
{
    public class StringLengthValidator : IValidator<string>
    {
        private readonly int _minLength;
        private readonly int _maxLength;
        private readonly bool _allowEmpty;

        public StringLengthValidator(FluxStringLengthAttribute attribute)
        {
            _minLength = attribute.MinLength;
            _maxLength = attribute.MaxLength;
            _allowEmpty = attribute.AllowEmpty;
        }

        public ValidationResult Validate(string value)
        {
            value = value ?? ""; // Treat null as empty
            
            if (!_allowEmpty && string.IsNullOrEmpty(value))
            {
                return ValidationResult.Failure("String cannot be null or empty.");
            }

            if (value.Length < _minLength || value.Length > _maxLength)
            {
                return ValidationResult.Failure($"String length {value.Length} is outside the allowed range [{_minLength}, {_maxLength}].");
            }
            
            return ValidationResult.Success;
        }
    }
}