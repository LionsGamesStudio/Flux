using System;

namespace FluxFramework.Attributes.VisualScripting
{
    /// <summary>
    /// Marks an INode class as a discoverable node for the visual scripting editor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class FluxNodeAttribute : Attribute
    {
        /// <summary>
        /// The name displayed in the node's header in the editor.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The category path for this node in the creation menu (e.g., "Math/Advanced").
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// A brief description of what the node does, often shown in the inspector or search window.
        /// </summary>
        public string Description { get; set; }

        public FluxNodeAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}