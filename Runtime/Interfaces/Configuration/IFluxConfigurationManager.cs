using System;
using System.Collections.Generic;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Configuration
{
    public interface IFluxConfigurationManager
    {
        /// <summary>
        /// Initializes the configuration manager and loads all configurations.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets a configuration asset of the specified type. Returns null if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetConfiguration<T>() where T : FluxConfigurationAsset;

        /// <summary>
        /// Gets a configuration asset of the specified type. Returns null if not found.
        /// </summary>
        /// <param name="configurationType"></param>
        /// <returns></returns>
        FluxConfigurationAsset GetConfiguration(Type configurationType);

        /// <summary>
        /// Gets all loaded configuration assets of the specified category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        List<FluxConfigurationAsset> GetConfigurationsByCategory(string category);

        /// <summary>
        /// Registers a new configuration asset. If a configuration of the same type already exists, it will be replaced.
        /// </summary>
        /// <param name="configuration"></param>
        void RegisterConfiguration(FluxConfigurationAsset configuration);

        /// <summary>
        /// Applies all loaded configurations to the specified FluxManager instance.
        /// </summary>
        /// <param name="manager"></param>
        void ApplyAllConfigurations(IFluxManager manager);

        /// <summary>
        /// Validates all loaded configurations and returns true if all are valid.
        /// </summary>
        /// <returns></returns>
        bool ValidateAllConfigurations();

        /// <summary>
        /// Gets information about all discovered configuration types
        /// </summary>
        /// <returns>Dictionary of configuration types and their attributes</returns>
        Dictionary<Type, FluxConfigurationAttribute> GetConfigurationTypes();

        /// <summary>
        /// Clear all cached data and force reinitialization
        /// </summary>
        void ClearCache();
    }
}