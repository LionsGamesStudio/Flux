using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// A configuration asset that allows for the declarative definition of global reactive properties.
    /// The framework will use this asset at startup to pre-register properties with their default values.
    /// </summary>
    [FluxConfiguration("Properties", 
        DisplayName = "Property Definitions", 
        Description = "Pre-defines global reactive properties and their default values.",
        LoadPriority = 90)] // Lower priority than Framework Settings
    [CreateAssetMenu(fileName = "FluxPropertyDefinitions", menuName = "Flux/Property Definitions")]
    public class FluxPropertyDefinitions : FluxConfigurationAsset
    {
        [Header("Property Definitions")]
        [Tooltip("A list of reactive properties to create when the application starts.")]
        public List<PropertyDefinition> properties = new List<PropertyDefinition>();

        /// <summary>
        /// Validates that all property definitions are correctly configured (e.g., no duplicate keys).
        /// </summary>
        public override bool ValidateConfiguration()
        {
            var keys = new HashSet<string>();
            foreach (var property in properties)
            {
                if (string.IsNullOrEmpty(property.key))
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error("[FluxFramework] A PropertyDefinition cannot have an empty key.", this);
                    return false;
                }
                if (!keys.Add(property.key))
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Duplicate property key found in definitions: '{property.key}'. Keys must be unique.", this);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Applies this configuration by iterating through the definitions and creating
        /// each reactive property in the central IFluxManager.
        /// </summary>
        public override void ApplyConfiguration(IFluxManager manager)
        {
            if (!ValidateConfiguration()) return;

            foreach (var propDef in properties)
            {
                try
                {
                    switch (propDef.type)
                    {
                        case PropertyType.Int:
                            int defaultInt = int.TryParse(propDef.defaultValue, out var i) ? i : 0;
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultInt);
                            break;
                            
                        case PropertyType.Float:
                            float defaultFloat = float.TryParse(propDef.defaultValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f;
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultFloat);
                            break;
                            
                        case PropertyType.Bool:
                            bool defaultBool = bool.TryParse(propDef.defaultValue, out var b) ? b : false;
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultBool);
                            break;
                            
                        case PropertyType.String:
                            manager.Properties.GetOrCreateProperty(propDef.key, propDef.defaultValue ?? "");
                            break;
                            
                        case PropertyType.Vector2:
                            Vector2 defaultV2 = Vector2.zero;
                            if (!string.IsNullOrEmpty(propDef.defaultValue))
                            {
                                try { defaultV2 = JsonUtility.FromJson<Vector2>(propDef.defaultValue); }
                                catch { /* Ignore malformed string, use Vector2.zero */ }
                            }
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultV2);
                            break;
                            
                        case PropertyType.Vector3:
                            Vector3 defaultV3 = Vector3.zero;
                            if (!string.IsNullOrEmpty(propDef.defaultValue))
                            {
                                try { defaultV3 = JsonUtility.FromJson<Vector3>(propDef.defaultValue); }
                                catch { /* Ignore malformed string, use Vector3.zero */ }
                            }
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultV3);
                            break;
                            
                        case PropertyType.Color:
                            Color defaultColor = Color.black; // Default fallback color
                            if (!string.IsNullOrEmpty(propDef.defaultValue))
                            {
                                ColorUtility.TryParseHtmlString(propDef.defaultValue, out defaultColor);
                            }
                            manager.Properties.GetOrCreateProperty(propDef.key, defaultColor);
                            break;
                        
                        // Note: Sprite cannot be created from a string. It would need a path to a resource.
                        case PropertyType.Sprite:
                            FluxFramework.Core.Flux.Manager.Logger.Warning($"[FluxFramework] Pre-defining properties of type 'Sprite' is not supported via string. Property '{propDef.key}' will be created with a null default value.", this);
                            manager.Properties.GetOrCreateProperty<Sprite>(propDef.key, null);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Failed to create pre-defined property '{propDef.key}' with value '{propDef.defaultValue}': {ex.Message}", this);
                }
            }

            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Applied {properties.Count} pre-defined properties.");

        }
    }
}