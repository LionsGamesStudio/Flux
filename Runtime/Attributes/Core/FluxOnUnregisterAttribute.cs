using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a method to be called when the component is unregistered
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FluxOnUnregisterAttribute : Attribute
    {
        /// <summary>
        /// Priority for execution order
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
