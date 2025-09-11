using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Exposes a method with parameters in the inspector, creating fields for each argument
    /// and a button to invoke the method with the specified values.
    /// This is intended for advanced debugging and testing directly from the editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FluxActionAttribute : Attribute
    {
        /// <summary>
        /// A custom display name for the action in the inspector. If null, the method name is used.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The text displayed on the invocation button.
        /// </summary>
        public string ButtonText { get; set; } = "Invoke";

        /// <summary>
        /// Marks a method as a configurable action in the inspector.
        /// </summary>
        /// <param name="displayName">An optional name to display in the inspector header for this action.</param>
        public FluxActionAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}