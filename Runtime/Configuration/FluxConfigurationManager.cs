using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Manages Flux configuration assets and their lifecycle
    /// </summary>
    public static class FluxConfigurationManager
    {
        private static Dictionary<Type, FluxConfigurationAsset> _loadedConfigurations = new Dictionary<Type, FluxConfigurationAsset>();
        private static Dictionary<string, List<Type>> _configurationsByCategory = new Dictionary<string, List<Type>>();
        private static Dictionary<Type, FluxConfigurationAttribute> _discoveredTypes = new Dictionary<Type, FluxConfigurationAttribute>();
        private static bool _isInitialized = false;
        private static bool _typesDiscovered = false;

        /// <summary>
        /// Initializes the configuration manager and discovers all configuration types
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            DiscoverConfigurationTypes();
            LoadConfigurations();
            _isInitialized = true;
            
            Debug.Log("[FluxFramework] Configuration Manager initialized");
        }

        /// <summary>
        /// Gets a configuration of the specified type
        /// </summary>
        /// <typeparam name="T">Type of configuration to retrieve</typeparam>
        /// <returns>The configuration instance or null if not found</returns>
        public static T GetConfiguration<T>() where T : FluxConfigurationAsset
        {
            return GetConfiguration(typeof(T)) as T;
        }

        /// <summary>
        /// Gets a configuration of the specified type
        /// </summary>
        /// <param name="configurationType">Type of configuration to retrieve</param>
        /// <returns>The configuration instance or null if not found</returns>
        public static FluxConfigurationAsset GetConfiguration(Type configurationType)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _loadedConfigurations.TryGetValue(configurationType, value: out var config);
            return config;
        }

        /// <summary>
        /// Gets all configurations in a specific category
        /// </summary>
        /// <param name="category">The category name</param>
        /// <returns>List of configuration instances</returns>
        public static List<FluxConfigurationAsset> GetConfigurationsByCategory(string category)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var configurations = new List<FluxConfigurationAsset>();
            
            if (_configurationsByCategory.TryGetValue(category, out var types))
            {
                foreach (var type in types)
                {
                    if (_loadedConfigurations.TryGetValue(type, out var config))
                    {
                        configurations.Add(config);
                    }
                }
            }

            return configurations;
        }

        /// <summary>
        /// Registers a configuration instance
        /// </summary>
        /// <param name="configuration">The configuration to register</param>
        public static void RegisterConfiguration(FluxConfigurationAsset configuration)
        {
            if (configuration == null) return;

            var type = configuration.GetType();
            _loadedConfigurations[type] = configuration;
            
            Debug.Log($"[FluxFramework] Registered configuration: {type.Name}");
        }

        /// <summary>
        /// Applies all loaded configurations
        /// </summary>
        public static void ApplyAllConfigurations()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Ensure we have a valid FluxManager instance to pass down.
            var manager = Flux.Manager as FluxManager;
            if (manager == null)
            {
                Debug.LogError("[FluxFramework] Cannot apply configurations because Flux.Manager is null.");
                return;
            }

            // Sort by load priority (higher priority first)
            var sortedConfigs = _loadedConfigurations.Values
                .OrderByDescending(config => GetLoadPriority(config.GetType()))
                .ToList();

            foreach (var config in sortedConfigs)
            {
                try
                {
                    config.ApplyConfiguration(manager);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Failed to apply configuration {config.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Validates all loaded configurations
        /// </summary>
        /// <returns>True if all configurations are valid</returns>
        public static bool ValidateAllConfigurations()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            bool allValid = true;

            foreach (var config in _loadedConfigurations.Values)
            {
                try
                {
                    if (!config.ValidateConfiguration())
                    {
                        Debug.LogError($"[FluxFramework] Configuration validation failed: {config.GetType().Name}");
                        allValid = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Exception during validation of {config.GetType().Name}: {ex.Message}");
                    allValid = false;
                }
            }

            return allValid;
        }

        /// <summary>
        /// Gets information about all discovered configuration types
        /// </summary>
        /// <returns>Dictionary of configuration types and their attributes</returns>
        public static Dictionary<Type, FluxConfigurationAttribute> GetConfigurationTypes()
        {
            if (!_typesDiscovered)
            {
                DiscoverConfigurationTypesOnce();
            }
            return new Dictionary<Type, FluxConfigurationAttribute>(_discoveredTypes);
        }

        private static void DiscoverConfigurationTypesOnce()
        {
            if (_typesDiscovered) return;

            _discoveredTypes.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Skip system assemblies for better performance
                    if (IsSystemAssembly(assembly.FullName))
                        continue;

                    var types = assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(FluxConfigurationAsset)) && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<FluxConfigurationAttribute>();
                        if (attribute != null)
                        {
                            _discoveredTypes[type] = attribute;
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"[FluxFramework] Could not load types from assembly {assembly.FullName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Silently ignore other exceptions to avoid spam
                    Debug.LogWarning($"[FluxFramework] Assembly scanning error: {ex.Message}");
                }
            }

            _typesDiscovered = true;
        }

        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") ||
                   assemblyName.StartsWith("Microsoft.") ||
                   assemblyName.StartsWith("Unity.") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName.StartsWith("UnityEditor") ||
                   assemblyName.StartsWith("mscorlib") ||
                   assemblyName.StartsWith("netstandard");
        }

        private static void DiscoverConfigurationTypes()
        {
            if (!_typesDiscovered)
            {
                DiscoverConfigurationTypesOnce();
            }

            _configurationsByCategory.Clear();

            foreach (var kvp in _discoveredTypes)
            {
                var type = kvp.Key;
                var attribute = kvp.Value;

                if (!_configurationsByCategory.ContainsKey(attribute.Category))
                {
                    _configurationsByCategory[attribute.Category] = new List<Type>();
                }

                _configurationsByCategory[attribute.Category].Add(type);
            }

            Debug.Log($"[FluxFramework] Discovered {_discoveredTypes.Count} configuration types in {_configurationsByCategory.Count} categories");
        }

        private static void LoadConfigurations()
        {
            // Load only configurations from known paths to avoid scanning entire Resources folder
            var loadedConfigs = new List<FluxConfigurationAsset>();
            
            // Try to load from specific configuration folders
            var configPaths = new[] { 
                "Configurations", 
                "Flux/Configurations", 
                "FluxFramework/Configurations" 
            };

            foreach (var path in configPaths)
            {
                try
                {
                    var configs = Resources.LoadAll<FluxConfigurationAsset>(path);
                    loadedConfigs.AddRange(configs);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FluxFramework] Could not load configurations from path '{path}': {ex.Message}");
                }
            }

            // Fallback: load from root if no configs found
            if (loadedConfigs.Count == 0)
            {
                var allConfigs = Resources.LoadAll<FluxConfigurationAsset>("");
                loadedConfigs.AddRange(allConfigs);
            }

            foreach (var config in loadedConfigs)
            {
                RegisterConfiguration(config);
            }

            // Auto-create required configurations that are missing
            foreach (var kvp in _discoveredTypes)
            {
                var type = kvp.Key;
                var attribute = kvp.Value;

                if (attribute.IsRequired && !_loadedConfigurations.ContainsKey(type))
                {
                    Debug.LogWarning($"[FluxFramework] Required configuration {type.Name} is missing!");
                }

                if (attribute.AutoCreate && !_loadedConfigurations.ContainsKey(type))
                {
                    try
                    {
                        var instance = ScriptableObject.CreateInstance(type) as FluxConfigurationAsset;
                        if (instance != null)
                        {
                            RegisterConfiguration(instance);
                            Debug.Log($"[FluxFramework] Auto-created configuration: {type.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FluxFramework] Failed to auto-create configuration {type.Name}: {ex.Message}");
                    }
                }
            }
        }

        private static int GetLoadPriority(Type configurationType)
        {
            var attribute = configurationType.GetCustomAttribute<FluxConfigurationAttribute>();
            return attribute?.LoadPriority ?? 0;
        }

        /// <summary>
        /// Clear all cached data and force reinitialization (Editor only)
        /// </summary>
        public static void ClearCache()
        {
            _loadedConfigurations.Clear();
            _configurationsByCategory.Clear();
            _discoveredTypes.Clear();
            _isInitialized = false;
            _typesDiscovered = false;
            
            Debug.Log("[FluxFramework] Configuration cache cleared");
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to refresh configurations during development
        /// </summary>
        [UnityEditor.MenuItem("Flux/Configuration/Clear Configuration Cache")]
        public static void EditorClearCache()
        {
            ClearCache();
        }
        #endif
    }
}
