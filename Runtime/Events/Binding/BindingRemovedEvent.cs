using System;

namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a UI binding is removed
    /// </summary>
    public class BindingRemovedEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Key of the property that was unbound
        /// </summary>
        public string PropertyKey { get; }

        /// <summary>
        /// Name of the component that was unbound
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Reason for the binding removal
        /// </summary>
        public string Reason { get; }

        public BindingRemovedEvent(string propertyKey, string componentName, string reason = "Manual")
            : base("FluxFramework.Binding.ReactiveBindingSystem")
        {
            PropertyKey = propertyKey;
            ComponentName = componentName;
            Reason = reason;
        }
    }
}
