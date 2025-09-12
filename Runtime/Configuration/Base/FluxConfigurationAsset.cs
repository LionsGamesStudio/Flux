using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// The abstract base class for all configuration assets in the Flux Framework.
    /// </summary>
    public abstract class FluxConfigurationAsset : ScriptableObject
    {
        /// <summary>
        /// Validates the settings within this configuration asset.
        /// </summary>
        /// <returns>True if the configuration is valid and can be applied.</returns>
        public abstract bool ValidateConfiguration();

        /// <summary>
        /// Applies the settings from this asset to the live framework systems.
        /// </summary>
        /// <param name="manager">The core FluxManager instance to configure.</param>
        public abstract void ApplyConfiguration(FluxManager manager);
    }
}