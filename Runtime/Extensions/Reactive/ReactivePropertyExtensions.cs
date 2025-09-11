using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Validation;

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
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="initialValue">The initial value, which must pass the validation.</param>
        /// <param name="validatorFunc">A function that returns true if the value is valid.</param>
        /// <returns>A new instance of ValidatedReactiveProperty.</returns>
        public static ValidatedReactiveProperty<T> WithValidation<T>(T initialValue, Func<T, bool> validatorFunc)
        {
            if (validatorFunc == null)
            {
                throw new ArgumentNullException(nameof(validatorFunc));
            }
            
            // We create a wrapper validator from the provided function and pass it to the new constructor.
            var wrapperValidator = new FuncValidator<T>(validatorFunc);
            var validators = new List<IValidator<T>> { wrapperValidator };
            return new ValidatedReactiveProperty<T>(initialValue, validators);
        }

        /// <summary>
        /// Creates a new reactive property whose value is a transformation of the source property.
        /// Also known as 'Select' or 'Map'.
        /// </summary>
        public static ReactiveProperty<TTarget> Transform<TSource, TTarget>(
            this ReactiveProperty<TSource> source, 
            Func<TSource, TTarget> transform)
        {
            var target = new ReactiveProperty<TTarget>(transform(source.Value));
            // The subscription will be managed by the new property's lifetime.
            source.Subscribe(value => target.Value = transform(value));
            return target;
        }

        /// <summary>
        /// Combines two reactive properties into a single new property using a combiner function.
        /// </summary>
        public static ReactiveProperty<TResult> CombineWith<T1, T2, TResult>(
            this ReactiveProperty<T1> prop1,
            ReactiveProperty<T2> prop2,
            Func<T1, T2, TResult> combiner)
        {
            var result = new ReactiveProperty<TResult>(combiner(prop1.Value, prop2.Value));
            
            prop1.Subscribe(value => result.Value = combiner(value, prop2.Value));
            prop2.Subscribe(value => result.Value = combiner(prop1.Value, value));
            
            return result;
        }

        /// <summary>
        /// Creates a new reactive property that only updates its value when the source value passes a filter condition.
        /// </summary>
        public static ReactiveProperty<T> Where<T>(this ReactiveProperty<T> source, Func<T, bool> filter)
        {
            var filtered = new ReactiveProperty<T>(source.Value);
            source.Subscribe(value =>
            {
                if (filter(value))
                {
                    filtered.Value = value;
                }
            });
            return filtered;
        }

        // NOTE: The simple Debounce and Delay implementations below are kept for reference,
        // but the more robust debouncing logic is now integrated into the ReactiveBindingSystem for UI.
        // A full reactive library would have more advanced schedulers for these operators.

        /// <summary>
        /// Creates a distinct reactive property that only notifies subscribers when its value actually changes.
        /// Note: The base ReactiveProperty already has this behavior, but this can be useful for chaining.
        /// </summary>
        public static ReactiveProperty<T> DistinctUntilChanged<T>(this ReactiveProperty<T> source)
        {
            // The base ReactiveProperty already includes an equality check.
            // This operator is more useful in advanced reactive streams where events might be duplicated.
            // We provide a simple implementation for API completeness.
            var distinct = new ReactiveProperty<T>(source.Value);
            source.Subscribe(value =>
            {
                // The base `Value` setter will handle the distinct check.
                distinct.Value = value;
            });
            return distinct;
        }

        /// <summary>
        /// A private helper class that wraps a simple Func<T, bool> into the IValidator<T> interface.
        /// </summary>
        private class FuncValidator<T> : IValidator<T>
        {
            private readonly Func<T, bool> _validationFunc;

            public FuncValidator(Func<T, bool> validationFunc)
            {
                _validationFunc = validationFunc;
            }

            public ValidationResult Validate(T value)
            {
                if (_validationFunc(value))
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return ValidationResult.Failure("Value did not pass the validation function.");
                }
            }
        }
    }
}