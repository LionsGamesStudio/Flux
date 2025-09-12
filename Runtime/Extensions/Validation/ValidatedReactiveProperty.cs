using System;
using System.Collections.Generic;
using System.Linq;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// A specialized ReactiveProperty that enforces validation rules before changing its value.
    /// It can be configured with multiple validators.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    public class ValidatedReactiveProperty<T> : ReactiveProperty<T>
    {
        private readonly List<IValidator<T>> _validators = new List<IValidator<T>>();

        /// <summary>
        /// Event raised when a new value is rejected by one of the validation rules.
        /// Provides the rejected value and the error messages.
        /// </summary>
        public event Action<T, string[]> OnValidationFailed;

        /// <summary>
        /// Initializes a new instance of the ValidatedReactiveProperty class.
        /// </summary>
        /// <param name="initialValue">The initial value, which must pass all validation rules.</param>
        /// <param name="validators">A collection of validators to apply to new values.</param>
        public ValidatedReactiveProperty(T initialValue, IEnumerable<IValidator<T>> validators) : base(initialValue)
        {
            if (validators != null)
            {
                _validators.AddRange(validators);
            }

            // Ensure the initial value is valid.
            if (!Validate(initialValue, out string[] errorMessages))
            {
                throw new ArgumentException($"Initial value '{initialValue}' does not pass validation. Errors: {string.Join(", ", errorMessages)}");
            }
        }
        
        /// <summary>
        /// Sets the value of the property, but only if it passes all validation rules.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        public override T Value
        {
            get => base.Value;
            set
            {
                if (Validate(value, out string[] errorMessages))
                {
                    // If valid, use the base class's setter to set the value and notify subscribers.
                    base.Value = value;
                }
                else
                {
                    // If invalid, invoke the failure event and log a warning.
                    OnValidationFailed?.Invoke(value, errorMessages);
                    Debug.LogWarning($"[FluxFramework] Value '{value}' for a ValidatedReactiveProperty was rejected. Errors: {string.Join(", ", errorMessages)}");
                }
            }
        }

        /// <summary>
        /// Checks if a given value is valid against all registered validators.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="errorMessages">An array of error messages if validation fails.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        public bool Validate(T value, out string[] errorMessages)
        {
            var errors = new List<string>();
            foreach (var validator in _validators)
            {
                var result = validator.Validate(value);
                if (!result.IsValid)
                {
                    errors.AddRange(result.ErrorMessages);
                }
            }

            errorMessages = errors.ToArray();
            return errors.Count == 0;
        }
    }
}