using UnityEngine;
using System.Collections.Generic;
using FluxFramework.Attributes;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Reactive property definitions
    /// </summary>
    [FluxConfiguration("Properties", 
        DisplayName = "Property Definitions", 
        Description = "Reactive property type definitions and default values",
        LoadPriority = 90,
        DefaultMenuPath = "Flux/Property Definitions")]
    [CreateAssetMenu(fileName = "FluxPropertyDefinitions", menuName = "Flux/Property Definitions")]
    public class FluxPropertyDefinitions : FluxConfigurationAsset
    {
        [Header("Property Definitions")]
        public List<PropertyDefinition> properties = new List<PropertyDefinition>();

        public override bool ValidateConfiguration()
        {
            var keys = new HashSet<string>();
            
            foreach (var property in properties)
            {
                if (string.IsNullOrEmpty(property.key))
                {
                    Debug.LogError("[FluxFramework] Property key cannot be empty");
                    return false;
                }

                if (keys.Contains(property.key))
                {
                    Debug.LogError($"[FluxFramework] Duplicate property key: {property.key}");
                    return false;
                }

                keys.Add(property.key);
            }

            return true;
        }

        public override void ApplyConfiguration()
        {
            if (!ValidateConfiguration()) return;

            // Initialize properties in the framework
            foreach (var property in properties)
            {
                Debug.Log($"[FluxFramework] Registered property: {property.key} ({property.type})");
            }
        }
    }
}
