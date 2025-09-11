using UnityEngine;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Base class for Flux configuration assets
    /// </summary>
    public abstract class FluxConfigurationAsset : ScriptableObject
    {
        /// <summary>
        /// Validates the configuration settings
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public abstract bool ValidateConfiguration();

        /// <summary>
        /// Applies the configuration to the framework
        /// </summary>
        public abstract void ApplyConfiguration();
    }
}
