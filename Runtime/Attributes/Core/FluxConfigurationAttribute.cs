using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a class as a Flux configuration asset
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class FluxConfigurationAttribute : Attribute
    {
        /// <summary>
        /// The category of the configuration (e.g., "Framework", "UI", "Events")
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The display name for the configuration in the editor
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description of what this configuration manages
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Priority for configuration loading (higher = loaded first)
        /// </summary>
        public int LoadPriority { get; set; } = 0;

        /// <summary>
        /// Whether this configuration is required for the framework to function
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// Whether this configuration should be automatically created if missing
        /// </summary>
        public bool AutoCreate { get; set; } = false;

        /// <summary>
        /// Default menu path for CreateAssetMenu if not specified elsewhere
        /// </summary>
        public string DefaultMenuPath { get; set; }

        /// <summary>
        /// Creates a new FluxConfiguration attribute
        /// </summary>
        /// <param name="category">The category of the configuration</param>
        public FluxConfigurationAttribute(string category)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
        }
    }
}
