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
    /// Provides advanced utilities to discover properties from code, including their default values.
    /// </summary>
    [CustomEditor(typeof(FluxPropertyDefinitions))]
    public class FluxPropertyDefinitionsEditor : UnityEditor.Editor
    {
        private FluxPropertyDefinitions _targetAsset;
        private string _categoryToScan = "General";

        private void OnEnable()
        {
            _targetAsset = (FluxPropertyDefinitions)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Automatic Discovery", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Scan Project for ANY New Properties"))
            {
                ScanAndAddProperties(null);
            }
            EditorGUILayout.HelpBox("Scans the project for [ReactiveProperty] attributes and adds any new keys found, attempting to retrieve their default values from code or prefabs.", MessageType.Info);
            
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Category-Specific Scan", EditorStyles.boldLabel);
            _categoryToScan = EditorGUILayout.TextField("Category to Scan", _categoryToScan);
            
            GUI.enabled = !string.IsNullOrWhiteSpace(_categoryToScan);
            if (GUILayout.Button($"Scan for Properties in '{_categoryToScan}' Category"))
            {
                ScanAndAddProperties(_categoryToScan);
            }
            GUI.enabled = true;
            EditorGUILayout.HelpBox("Scans for properties matching the specified category and adds them to this asset if they are not already defined elsewhere.", MessageType.Info);
        }

        /// <summary>
        /// Scans the project and adds property definitions, now with default value retrieval.
        /// </summary>
        /// <param name="categoryFilter">If not null, only properties with a matching Category will be added.</param>
        private void ScanAndAddProperties(string categoryFilter)
        {
            int keysAdded = 0;
            
            // 1. Get a set of all keys that are already defined in ANY definitions asset to prevent duplicates.
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
            
            // 2. Scan all relevant types (MonoBehaviours and ScriptableObjects) for the [ReactiveProperty] attribute.
            var typesToScan = TypeCache.GetTypesDerivedFrom<FluxMonoBehaviour>()
                                .Concat(TypeCache.GetTypesDerivedFrom<FluxScriptableObject>());

            var definitionsToAdd = new List<PropertyDefinition>();

            foreach (var type in typesToScan)
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                    if (reactiveAttr == null || reactiveAttr.ExcludeFromDiscovery || string.IsNullOrEmpty(reactiveAttr.Key) || allDefinedKeys.Contains(reactiveAttr.Key))
                    {
                        continue;
                    }
                    
                    if (categoryFilter != null && reactiveAttr.Category != categoryFilter)
                    {
                        continue;
                    }
                    
                    // 3. Attempt to get the default value for the discovered field.
                    object defaultValue = GetDefaultValueForField(type, field);

                    // 4. Create the new definition with all the retrieved info.
                    var newDefinition = new PropertyDefinition
                    {
                        key = reactiveAttr.Key,
                        type = GetPropertyTypeFromField(field),
                        description = $"Discovered from {type.Name}.{field.Name}",
                        defaultValue = ConvertValueToString(defaultValue)
                    };

                    definitionsToAdd.Add(newDefinition);
                    allDefinedKeys.Add(reactiveAttr.Key); // Add to set to prevent adding duplicates from the same scan.
                    keysAdded++;
                }
            }

            if (keysAdded > 0)
            {
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

        /// <summary>
        /// Attempts to retrieve the default value of a field by inspecting ScriptableObjects or Prefabs.
        /// If no instance can be found, it falls back to the default value for the field's type.
        /// </summary>
        private object GetDefaultValueForField(Type ownerType, FieldInfo field)
        {
            try
            {
                // Case 1: The owner is a ScriptableObject.
                if (typeof(ScriptableObject).IsAssignableFrom(ownerType))
                {
                    var tempInstance = ScriptableObject.CreateInstance(ownerType);
                    object value = field.GetValue(tempInstance);
                    DestroyImmediate(tempInstance); // Clean up the temporary instance.
                    return value;
                }
                
                // Case 2: The owner is a MonoBehaviour.
                if (typeof(MonoBehaviour).IsAssignableFrom(ownerType))
                {
                    string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    foreach (var guid in prefabGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            var component = prefab.GetComponentInChildren(ownerType, true);
                            if (component != null)
                            {
                                // We found a prefab with the component. Use its value as the default.
                                return field.GetValue(component);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FluxFramework] Could not retrieve default value for '{field.Name}' in '{ownerType.Name}': {ex.Message}");
            }

            // --- FALLBACK LOGIC ---
            // If no instance was found, fall back to the C# default for the field's value type.
            Debug.Log($"[FluxFramework] Could not find a prefab instance for component '{ownerType.Name}'. Falling back to the type's default value for field '{field.Name}'.");
            
            Type valueType = GetValueTypeFromFieldInfo(field);
            if (valueType.IsValueType)
            {
                return Activator.CreateInstance(valueType); // e.g., 0 for int, false for bool
            }
            
            return null; // e.g., null for string or other reference types
        }
        
        /// <summary>
        /// Converts a retrieved default value object into a string suitable for serialization in the definition asset.
        /// </summary>
        private string ConvertValueToString(object value)
        {
            if (value == null) return "";
            
            Type type = value.GetType();
            
            if (type == typeof(string)) return (string)value;
            if (type == typeof(Color)) return "#" + ColorUtility.ToHtmlStringRGB((Color)value);
            if (type == typeof(Vector2) || type == typeof(Vector3)) return JsonUtility.ToJson(value);
            
            // For primitives (int, float, bool), ToString() is reliable.
            // For bool, we want "true" or "false" in lowercase to be consistent with C#.
            if (type == typeof(bool)) return value.ToString().ToLowerInvariant();
            
            return value.ToString();
        }

        /// <summary>
        /// Gets the framework's PropertyType enum from a C# FieldInfo.
        /// </summary>
        private PropertyType GetPropertyTypeFromField(FieldInfo field)
        {
            Type valueType = GetValueTypeFromFieldInfo(field);

            if (valueType == typeof(int)) return PropertyType.Int;
            if (valueType == typeof(float)) return PropertyType.Float;
            if (valueType == typeof(bool)) return PropertyType.Bool;
            if (valueType == typeof(string)) return PropertyType.String;
            if (valueType == typeof(Vector2)) return PropertyType.Vector2;
            if (valueType == typeof(Vector3)) return PropertyType.Vector3;
            if (valueType == typeof(Color)) return PropertyType.Color;
            if (valueType == typeof(Sprite)) return PropertyType.Sprite;
            return PropertyType.String; // Default fallback
        }

        /// <summary>
        /// A helper to get the actual value type from a FieldInfo,
        /// correctly handling both implicit (int) and explicit (ReactiveProperty<int>) patterns.
        /// </summary>
        private Type GetValueTypeFromFieldInfo(FieldInfo field)
        {
            Type fieldType = field.FieldType;
            if (typeof(IReactiveProperty).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
            {
                return fieldType.GetGenericArguments()[0];
            }
            return fieldType;
        }
    }
}