using System;

namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a UI binding is created between a component and a property
    /// </summary>
    public class BindingCreatedEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Key of the property being bound
        /// </summary>
        public string PropertyKey { get; }

        /// <summary>
        /// Name of the component being bound
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Type of binding (OneWay, TwoWay, OneTime)
        /// </summary>
        public string BindingType { get; }

        /// <summary>
        /// GameObject instance ID of the bound component
        /// </summary>
        public int ComponentInstanceId { get; }

        public BindingCreatedEvent(string propertyKey, string componentName, string bindingType, int componentInstanceId)
            : base("FluxFramework.Binding.ReactiveBindingSystem")
        {
            PropertyKey = propertyKey;
            ComponentName = componentName;
            BindingType = bindingType;
            ComponentInstanceId = componentInstanceId;
        }
    }
}
