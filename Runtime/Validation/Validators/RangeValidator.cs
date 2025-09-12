using FluxFramework.Attributes;
using FluxFramework.Core;
using UnityEngine;
using System;

namespace FluxFramework.Validation
{
    public class RangeValidator<T> : IValidator<T> where T : System.IComparable
    {
        private readonly T _min;
        private readonly T _max;

        public RangeValidator(T min, T max)
        {
            _min = min;
            _max = max;
        }

        public ValidationResult Validate(T value)
        {
            if (value.CompareTo(_min) < 0 || value.CompareTo(_max) > 0)
            {
                return ValidationResult.Failure($"Value {value} is outside the allowed range [{_min}, {_max}].");
            }
            return ValidationResult.Success;
        }
    }
}