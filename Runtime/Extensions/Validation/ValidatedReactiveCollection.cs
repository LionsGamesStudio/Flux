using System;
using System.Collections.Generic;
using FluxFramework.Core;
using FluxFramework.Validation;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// A reactive collection that also validates its entire list content before accepting a change.
    /// </summary>
    [Serializable]
    public class ValidatedReactiveCollection<T> : ReactiveCollection<T>
    {
        private readonly List<IValidator<List<T>>> _validators;
        private ValidationResult _lastValidationResult = ValidationResult.Success;

        public event Action<ValidationResult> OnValidationStateChanged;

        public ValidationResult LastValidationResult => _lastValidationResult;
        public bool IsValid => _lastValidationResult.IsValid;

        public ValidatedReactiveCollection(List<T> initialValue, IEnumerable<IValidator<List<T>>> validators)
            : base(initialValue)
        {
            _validators = new List<IValidator<List<T>>>(validators ?? throw new ArgumentNullException(nameof(validators)));
            Validate(initialValue); // Validate the initial state.
        }

        // Override the Value property to include validation logic.
        public override List<T> Value
        {
            get => base.Value;
            set
            {
                if (Validate(value))
                {
                    base.Value = value;
                }
            }
        }
        
        private bool Validate(List<T> valueToValidate)
        {
            ValidationResult newResult = ValidationResult.Success;
            foreach (var validator in _validators)
            {
                newResult = validator.Validate(valueToValidate);
                if (!newResult.IsValid)
                {
                    break; // Stop at the first validation failure.
                }
            }
            
            if (!_lastValidationResult.Equals(newResult))
            {
                _lastValidationResult = newResult;
                OnValidationStateChanged?.Invoke(_lastValidationResult);
            }
            
            return _lastValidationResult.IsValid;
        }
    }
}