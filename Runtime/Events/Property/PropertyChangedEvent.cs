using System;

namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a reactive property value changes
    /// </summary>
    public class PropertyChangedEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Unique key of the property that changed
        /// </summary>
        public string PropertyKey { get; }

        /// <summary>
        /// Previous value of the property
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// New value of the property
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Type of the property value
        /// </summary>
        public Type ValueType { get; }

        public PropertyChangedEvent(string propertyKey, object oldValue, object newValue, Type valueType)
            : base("FluxFramework.Core.PropertyManager")
        {
            PropertyKey = propertyKey;
            OldValue = oldValue;
            NewValue = newValue;
            ValueType = valueType;
        }
    }
}
