using System;
using UnityEngine;
using FluxFramework.Validation;
using FluxFramework.Core;
using System.Reflection;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Abstract base class for all validation attributes in the Flux Framework.
    /// It establishes a contract that forces each attribute to know how to create its associated validator.
    /// It inherits from PropertyAttribute to maintain compatibility with Unity's editor and serialization system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class FluxValidationAttribute : PropertyAttribute
    {
        /// <summary>
        /// A custom validation message that can be used by UI components or loggers.
        /// </summary>
        public string ValidationMessage { get; set; }

        /// <summary>
        /// Creates an instance of the validator associated with this attribute.
        /// </summary>
        /// <param name="field">The field that this attribute is decorating. Used to infer the target type for the validator (e.g., int, float, string).</param>
        /// <returns>An instance of a class implementing IValidator, or null if creation fails.</returns>
        public abstract IValidator CreateValidator(FieldInfo field);
    }
}