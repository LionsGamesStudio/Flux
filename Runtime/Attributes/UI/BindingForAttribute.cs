using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Associates a UI Binding class with a specific Unity Component type.
    /// This allows the BindingFactory to automatically discover and register binding creators.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BindingForAttribute : Attribute
    {
        /// <summary>
        /// The type of the Unity Component that this binding is designed for (e.g., typeof(Slider), typeof(TextMeshProUGUI)).
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Initializes a new instance of the BindingForAttribute class.
        /// </summary>
        /// <param name="componentType">The type of the Unity Component that this binding targets.</param>
        public BindingForAttribute(Type componentType)
        {
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("The provided type must be a subclass of UnityEngine.Component.", nameof(componentType));
            }
            ComponentType = componentType;
        }
    }
}