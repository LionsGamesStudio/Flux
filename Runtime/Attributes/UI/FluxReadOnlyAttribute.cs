using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a field or property as read-only in the Flux inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxReadOnlyAttribute : PropertyAttribute
    {
        /// <summary>
        /// Message to display in the inspector
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Whether to show the field grayed out or hide it completely
        /// </summary>
        public bool GrayOut { get; set; } = true;

        public FluxReadOnlyAttribute(string message = null)
        {
            Message = message;
        }
    }
}
