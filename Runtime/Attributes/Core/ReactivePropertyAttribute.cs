using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a field as a reactive property that should be automatically bound to the framework
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReactivePropertyAttribute : PropertyAttribute
    {
        /// <summary>
        /// Unique key for the reactive property
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Whether the property should be automatically initialized
        /// </summary>
        public bool AutoInitialize { get; set; } = true;

        /// <summary>
        /// Whether the property should be thread-safe
        /// </summary>
        public bool ThreadSafe { get; set; } = true;

        /// <summary>
        /// Description for documentation and editor display
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Whether to save this property value between sessions
        /// </summary>
        public bool Persistent { get; set; } = false;

        /// <summary>
        /// Default value if not initialized
        /// </summary>
        public object DefaultValue { get; set; }

        public ReactivePropertyAttribute(string key)
        {
            Key = key;
        }
    }
}
