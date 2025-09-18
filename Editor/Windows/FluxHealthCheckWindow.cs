using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.UI;
using FluxFramework.Configuration;
using System.Linq;

namespace FluxFramework.Editor
{
    /// <summary>
    /// An editor window that scans the project for common issues, such as broken or invalid
    /// property bindings, to help maintain project health and prevent runtime errors.
    /// </summary>
    public class FluxHealthCheckWindow : EditorWindow
    {
        // --- DATA STRUCTURES ---
        private class ScanResult
        {
            public string Message;
            public UnityEngine.Object Context; // The component or asset where the issue was found
            public MessageType Type;
        }

        private HashSet<string> _definedPropertyKeys = new HashSet<string>();
        private HashSet<string> _discoveredPropertyKeys = new HashSet<string>();
        private List<ScanResult> _results = new List<ScanResult>();
        private Vector2 _scrollPosition;
        private bool _hasScanned = false;

        public static void ShowWindow()
        {
            GetWindow<FluxHealthCheckWindow>("Flux Health Check");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Flux Project Health Check", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool scans your project for common issues, like broken property bindings used in [FluxBinding] attributes or Flux UI Components.", MessageType.Info);

            if (GUILayout.Button("Scan Project For Issues"))
            {
                ScanProject();
            }

            if (_hasScanned)
            {
                DisplayResults();
            }
        }

        /// <summary>
        /// The main entry point for the project scanning logic.
        /// </summary>
        private void ScanProject()
        {
            _hasScanned = true;
            _definedPropertyKeys.Clear();
            _discoveredPropertyKeys.Clear();
            _results.Clear();

            // --- STEP 1: Load DEFINED keys from definition assets (Primary Source of Truth) ---
            string[] definitionGuids = AssetDatabase.FindAssets($"t:{nameof(FluxPropertyDefinitions)}");
            foreach (string guid in definitionGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definitionsAsset = AssetDatabase.LoadAssetAtPath<FluxPropertyDefinitions>(path);
                if (definitionsAsset != null)
                {
                    foreach (var propDef in definitionsAsset.properties)
                    {
                        if (!string.IsNullOrEmpty(propDef.key))
                        {
                            _definedPropertyKeys.Add(propDef.key);
                        }
                    }
                }
            }
            _results.Add(new ScanResult { Message = $"Found {_definedPropertyKeys.Count} keys in Property Definition assets.", Type = MessageType.Info });

            // --- STEP 2: DISCOVER all other keys directly from code ([ReactiveProperty] attributes) ---
            var allMonoBehaviours = GetAllAssetsOfType<MonoBehaviour>();
            var allScriptableObjects = GetAllAssetsOfType<ScriptableObject>();
            foreach (var mb in allMonoBehaviours) FindDiscoveredKeysInObject(mb);
            foreach (var so in allScriptableObjects) FindDiscoveredKeysInObject(so);
            
            // --- STEP 2a: Report on keys that are in code but not in official definitions ---
            foreach (var discoveredKey in _discoveredPropertyKeys)
            {
                if (!_definedPropertyKeys.Contains(discoveredKey))
                {
                    _results.Add(new ScanResult {
                        Message = $"Discovered key '{discoveredKey}' exists in code but is not in any Property Definition asset. Consider running the property scanner tool.",
                        Type = MessageType.Warning
                    });
                }
            }

            // The complete set of valid keys is the union of both sources for maximum coverage.
            var allValidKeys = new HashSet<string>(_definedPropertyKeys);
            allValidKeys.UnionWith(_discoveredPropertyKeys);

            // --- STEP 3: Check Consumers (Bindings) against the complete list of valid keys ---
            int issueCount = 0;
            foreach (var mb in allMonoBehaviours)
            {
                issueCount += CheckBindingsInObject(mb, allValidKeys);
            }

            if (issueCount == 0)
            {
                _results.Add(new ScanResult { Message = "No binding issues found. Everything looks healthy!", Type = MessageType.Info });
            }
            else
            {
                _results.Insert(0, new ScanResult { Message = $"Scan complete. Found {issueCount} binding issue(s).", Type = MessageType.Error });
            }
        }
        
        /// <summary>
        /// Displays the list of scan results in a scrollable view.
        /// </summary>
        private void DisplayResults()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scan Results:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            foreach (var result in _results)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(result.Message, result.Type, true);
                if (result.Context != null)
                {
                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(38)))
                    {
                        Selection.activeObject = result.Context;
                        EditorGUIUtility.PingObject(result.Context);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        #region Scanning Logic

        /// <summary>
        /// Scans a single object for fields with [ReactiveProperty] and adds their keys to the discovered list.
        /// </summary>
        private void FindDiscoveredKeysInObject(object obj)
        {
            if (obj == null) return;
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null && !string.IsNullOrEmpty(reactiveAttr.Key))
                {
                    _discoveredPropertyKeys.Add(reactiveAttr.Key);
                }
            }
        }

        /// <summary>
        /// Scans a single MonoBehaviour for broken bindings and returns the number of issues found.
        /// </summary>
        private int CheckBindingsInObject(MonoBehaviour mb, HashSet<string> allValidKeys)
        {
            if (mb == null) return 0;
            int issuesFound = 0;
            var fields = mb.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // Case 1: Check fields with the [FluxBinding] attribute.
                var bindingAttr = field.GetCustomAttribute<FluxBindingAttribute>();
                if (bindingAttr != null)
                {
                    if (string.IsNullOrEmpty(bindingAttr.PropertyKey))
                    {
                        _results.Add(new ScanResult { Message = $"[FluxBinding] on field '{field.Name}' has an empty property key.", Context = mb, Type = MessageType.Error });
                        issuesFound++;
                    }
                    else if (!allValidKeys.Contains(bindingAttr.PropertyKey))
                    {
                        _results.Add(new ScanResult { Message = $"[FluxBinding] on field '{field.Name}' uses an invalid or misspelled key: '{bindingAttr.PropertyKey}'.", Context = mb, Type = MessageType.Error });
                        issuesFound++;
                    }
                }

                // Case 2: Check for custom FluxUIComponents (like FluxSlider, FluxText) that have serialized private string fields for property keys.
                if (mb is FluxUIComponent)
                {
                    if (field.Name.ToLower().Contains("propertykey") && field.IsSerialized())
                    {
                        try
                        {
                            var key = field.GetValue(mb) as string;
                            if (!string.IsNullOrEmpty(key) && !allValidKeys.Contains(key))
                            {
                                _results.Add(new ScanResult { Message = $"Component '{mb.GetType().Name}' uses an invalid or misspelled key in its Inspector field '{field.Name}': '{key}'.", Context = mb, Type = MessageType.Error });
                                issuesFound++;
                            }
                        }
                        catch { /* Ignore fields that can't be read */ }
                    }
                }
            }
            return issuesFound;
        }

        #endregion

        #region Utility

        /// <summary>
        /// A helper function to find all assets of a specific type (including prefabs and ScriptableObjects).
        /// </summary>
        private List<T> GetAllAssetsOfType<T>() where T : UnityEngine.Object
        {
            var assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(T));

                if (asset != null)
                {
                    if (asset is GameObject go)
                    {
                        // A GameObject asset (a prefab) was found.
                        // ONLY try to get components from it if T is a Component type.
                        if (typeof(Component).IsAssignableFrom(typeof(T)))
                        {
                            assets.AddRange(go.GetComponentsInChildren<T>(true));
                        }
                        // If T is not a component (like ScriptableObject), we simply ignore this GameObject asset.
                    }
                    else if (asset is T typedAsset)
                    {
                        // The asset itself is of type T (e.g., a ScriptableObject), so add it directly.
                        assets.Add(typedAsset);
                    }
                }
            }

            // Also include all components from the currently open scenes.
            // This part is safe because it only runs on GameObjects in the scene hierarchy.
            if (typeof(Component).IsAssignableFrom(typeof(T)))
            {
                for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
                {
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;
                    
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        foreach(var component in root.GetComponentsInChildren<T>(true))
                        {
                            assets.Add(component);
                        }
                    }
                }
            }
            return assets.Distinct().ToList();
        }
        
        #endregion
        }
    
    /// <summary>
    /// A small helper extension method to check if a field is serialized by Unity.
    /// </summary>
    public static class FieldInfoExtensions
    {
        public static bool IsSerialized(this FieldInfo field)
        {
            return field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
        }
    }
}