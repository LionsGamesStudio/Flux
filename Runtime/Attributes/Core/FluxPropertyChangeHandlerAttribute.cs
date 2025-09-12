using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a method as a handler for a ReactiveProperty's value change.
    /// The framework will automatically find this method and subscribe it to the
    /// specified property's OnValueChanged event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FluxPropertyChangeHandlerAttribute : Attribute
    {
        /// <summary>
        /// The unique key of the ReactiveProperty to listen to.
        /// </summary>
        public string PropertyKey { get; }

        public FluxPropertyChangeHandlerAttribute(string propertyKey)
        {
            if (string.IsNullOrEmpty(propertyKey))
            {
                throw new ArgumentNullException(nameof(propertyKey), "PropertyKey cannot be null or empty.");
            }
            PropertyKey = propertyKey;
        }
    }
}