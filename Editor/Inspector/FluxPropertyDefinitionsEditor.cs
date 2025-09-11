using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using FluxFramework.Configuration;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A custom editor for FluxPropertyDefinitions assets.
    /// Provides utilities to automatically discover and add property definitions from code.
    /// </summary>
    [CustomEditor(typeof(FluxPropertyDefinitions))]
    public class FluxPropertyDefinitionsEditor : UnityEditor.Editor
    {
        private FluxPropertyDefinitions _targetAsset;
        private string _categoryToScan = "General"; // For the category-specific scan

        private void OnEnable()
        {
            _targetAsset = (FluxPropertyDefinitions)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Automatic Discovery", EditorStyles.boldLabel);
            
            // --- GENERAL SCAN BUTTON ---
            if (GUILayout.Button("Scan for ANY New Properties"))
            {
                ScanAndAddProperties(null); // Pass null to scan for all categories
            }
            EditorGUILayout.HelpBox("Scans the entire project for [ReactiveProperty] attributes and adds any key that is not already defined in *any* definitions asset.", MessageType.Info);
            
            EditorGUILayout.Space();

            // --- CATEGORY-SPECIFIC SCAN ---
            EditorGUILayout.LabelField("Category-Specific Scan", EditorStyles.boldLabel);
            _categoryToScan = EditorGUILayout.TextField("Category to Scan", _categoryToScan);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(_categoryToScan);
            if (GUILayout.Button($"Scan for Properties in '{_categoryToScan}' Category"))
            {
                ScanAndAddProperties(_categoryToScan);
            }
            GUI.enabled = true;
            EditorGUILayout.HelpBox("Scans for properties matching the specified category and adds them here if they are not already defined elsewhere.", MessageType.Info);
        }

        /// <summary>
        /// Scans the project and adds property definitions to the current asset.
        /// </summary>
        /// <param name="categoryFilter">If not null, only adds properties with a matching Category.</param>
        private void ScanAndAddProperties(string categoryFilter)
        {
            int keysAdded = 0;
            
            // 1. Get a set of all keys that are already defined in ANY definitions asset.
            var allDefinedKeys = new HashSet<string>();
            string[] allGuids = AssetDatabase.FindAssets($"t:{nameof(FluxPropertyDefinitions)}");
            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FluxPropertyDefinitions>(path);
                if (asset != null)
                {
                    foreach (var prop in asset.properties) allDefinedKeys.Add(prop.key);
                }
            }
            
            // 2. Scan all relevant types for the [ReactiveProperty] attribute.
            var typesToScan = TypeCache.GetTypesDerivedFrom<FluxMonoBehaviour>()
                                .Concat(TypeCache.GetTypesDerivedFrom<FluxScriptableObject>());

            var definitionsToAdd = new List<PropertyDefinition>();

            foreach (var type in typesToScan)
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                    if (reactiveAttr == null) continue;
                    
                    // 3. Apply all filters.
                    if (reactiveAttr.ExcludeFromDiscovery) continue;
                    if (!string.IsNullOrEmpty(reactiveAttr.Key) && !allDefinedKeys.Contains(reactiveAttr.Key))
                    {
                        // If a category filter is active, check if the attribute's category matches.
                        if (categoryFilter != null && reactiveAttr.Category != categoryFilter)
                        {
                            continue;
                        }

                        // 4. If all checks pass, create the new definition.
                        var newDefinition = new PropertyDefinition
                        {
                            key = reactiveAttr.Key,
                            type = GetPropertyTypeFromField(field.FieldType),
                            description = $"Discovered from {type.Name}.{field.Name}",
                            defaultValue = "" // Default value is too hard to get reliably, leave it for the user.
                        };

                        definitionsToAdd.Add(newDefinition);
                        allDefinedKeys.Add(reactiveAttr.Key); // Add to the set to avoid duplicates within the same scan.
                        keysAdded++;
                    }
                }
            }

            if (keysAdded > 0)
            {
                // 5. Add the new definitions to the target asset and save.
                _targetAsset.properties.AddRange(definitionsToAdd);
                EditorUtility.SetDirty(_targetAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[FluxFramework] Discovery complete. Added {keysAdded} new property definition(s) to '{_targetAsset.name}'.", _targetAsset);
            }
            else
            {
                Debug.Log("[FluxFramework] Discovery complete. No new property definitions found.", _targetAsset);
            }
        }
        
        private PropertyType GetPropertyTypeFromField(Type fieldType)
        {
            if (fieldType == typeof(int)) return PropertyType.Int;
            if (fieldType == typeof(float)) return PropertyType.Float;
            if (fieldType == typeof(bool)) return PropertyType.Bool;
            if (fieldType == typeof(string)) return PropertyType.String;
            if (fieldType == typeof(Vector2)) return PropertyType.Vector2;
            if (fieldType == typeof(Vector3)) return PropertyType.Vector3;
            if (fieldType == typeof(Color)) return PropertyType.Color;
            if (fieldType == typeof(Sprite)) return PropertyType.Sprite;
            return PropertyType.String; // Fallback
        }
    }
}