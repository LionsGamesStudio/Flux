using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a method to be called when the component is registered
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FluxOnRegisterAttribute : Attribute
    {
        /// <summary>
        /// Priority for execution order
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
