using System;
using UnityEngine;
using FluxFramework.Binding;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a field as a UI binding property
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxBindingAttribute : PropertyAttribute
    {
        /// <summary>
        /// Property key to bind to
        /// </summary>
        public string PropertyKey { get; }

        /// <summary>
        /// Type of binding (OneWay, TwoWay, OneTime)
        /// </summary>
        public BindingMode Mode { get; set; } = BindingMode.OneWay;

        /// <summary>
        /// Converter type for value conversion
        /// </summary>
        public Type ConverterType { get; set; }

        /// <summary>
        /// Whether to update immediately on change
        /// </summary>
        public bool ImmediateUpdate { get; set; } = true;

        /// <summary>
        /// Delay in milliseconds before updating (debouncing)
        /// </summary>
        public int UpdateDelay { get; set; } = 0;

        public FluxBindingAttribute(string propertyKey)
        {
            PropertyKey = propertyKey;
        }

        /// <summary>
        /// Creates a BindingOptions object from the attribute's properties.
        /// </summary>
        public BindingOptions CreateOptions()
        {
            IValueConverter converter = null;
            if (ConverterType != null)
            {
                // Ensure the type is a valid IValueConverter
                if (typeof(IValueConverter).IsAssignableFrom(ConverterType))
                {
                    try { converter = Activator.CreateInstance(ConverterType) as IValueConverter; }
                    catch (Exception ex) { UnityEngine.Debug.LogError($"[FluxFramework] Could not create instance of converter '{ConverterType.Name}': {ex.Message}"); }
                }
            }

            return new BindingOptions
            {
                Mode = this.Mode,
                UpdateDelayMs = this.UpdateDelay,
                Converter = converter,
                ImmediateUpdate = this.ImmediateUpdate
            };
        }
    }

    /// <summary>
    /// Binding modes for UI properties
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// Property to UI only
        /// </summary>
        OneWay,
        
        /// <summary>
        /// UI to property only
        /// </summary>
        OneWayToSource,
        
        /// <summary>
        /// Bidirectional binding
        /// </summary>
        TwoWay,
        
        /// <summary>
        /// One-time binding (no updates)
        /// </summary>
        OneTime
    }
}
