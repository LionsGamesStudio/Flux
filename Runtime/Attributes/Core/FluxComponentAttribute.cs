using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a class as a Flux component for automatic registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FluxComponentAttribute : Attribute
    {
        /// <summary>
        /// Whether to automatically register this component with the framework
        /// </summary>
        public bool AutoRegister { get; set; } = true;

        /// <summary>
        /// Category for organizing components in the editor
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Description for the component
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this component requires the framework to be initialized
        /// </summary>
        public bool RequireFramework { get; set; } = true;

        /// <summary>
        /// Priority for initialization order (higher values initialize first)
        /// </summary>
        public int InitializationPriority { get; set; } = 0;
    }
}
