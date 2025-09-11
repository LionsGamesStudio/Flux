using UnityEngine;
using FluxFramework.Core;
using System;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Base class for UI bindings
    /// </summary>
    /// <typeparam name="T">Type of the bound value</typeparam>
    public abstract class UIBinding<T> : IUIBinding<T>
    {
        public Component Component { get; private set; }
        public string PropertyKey { get; private set; }
        public bool IsActive { get; protected set; } = true;

        protected BindingOptions Options { get; private set; }

        protected UIBinding(string propertyKey, Component component)
        {
            PropertyKey = propertyKey;
            Component = component;
            Options = BindingOptions.Default;
        }

        public abstract void UpdateUI(T value);
        public abstract T GetUIValue();

        public Type ValueType => typeof(T);

        /// <summary>
        /// Gets the current value from the UI as object (implements IUIBinding.GetUIValue)
        /// </summary>
        /// <returns>Current UI value as object</returns>
        object IUIBinding.GetUIValue() => GetUIValue();

        /// <summary>
        /// Updates UI from object value (implements IUIBinding.UpdateUI)
        /// </summary>
        /// <param name="value">Object value to convert and update</param>
        public virtual void UpdateUI(object value)
        {
            if (value is T typedValue)
            {
                UpdateUI(typedValue);
            }
            else if (value == null && !typeof(T).IsValueType)
            {
                UpdateUI(default(T));
            }
            else
            {
                throw new System.ArgumentException($"Value must be of type {typeof(T)} or null");
            }
        }

        /// <summary>
        /// Sets the binding options after construction.
        /// This is called by the ReactiveBindingSystem during the Bind process.
        /// </summary>
        public virtual void SetOptions(BindingOptions options)
        {
            Options = options ?? BindingOptions.Default;
        }

        public virtual void UpdateFromProperty()
        {
            // Get the property from the central manager.
            var property = FluxManager.Instance.GetProperty<T>(PropertyKey);
            if (property != null)
            {
                // Get the raw value.
                T rawValue = property.Value;
                
                // Apply the converter if one exists in the stored options.
                object finalValue = Options.Converter != null ? Options.Converter.Convert(rawValue) : rawValue;
                
                // Update the UI with the final (potentially converted) value.
                UpdateUI((T)finalValue);
            }
        }

        public virtual void UpdateToProperty()
        {
            // Implementation should be provided in derived classes
        }

        public virtual void Dispose()
        {
            IsActive = false;
            // Override in derived classes for cleanup
        }
    }
}
