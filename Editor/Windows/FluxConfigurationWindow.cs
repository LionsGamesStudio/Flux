using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FluxFramework.Configuration;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Editor window for managing Flux configurations
    /// </summary>
    public class FluxConfigurationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _categoryFoldouts = new Dictionary<string, bool>();

        [MenuItem("Flux/Configuration Manager")]
        public static void ShowWindow()
        {
            GetWindow<FluxConfigurationWindow>("Flux Configurations");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Flux Framework Configurations", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh Configurations"))
            {
                FluxConfigurationManager.Initialize();
                Repaint();
            }

            if (GUILayout.Button("Validate All Configurations"))
            {
                bool allValid = FluxConfigurationManager.ValidateAllConfigurations();
                EditorUtility.DisplayDialog("Validation Result", 
                    allValid ? "All configurations are valid!" : "Some configurations have errors. Check the console.", 
                    "OK");
            }

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawConfigurationsByCategory();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigurationsByCategory()
        {
            var configTypes = FluxConfigurationManager.GetConfigurationTypes();
            var categorizedTypes = configTypes.GroupBy(kvp => kvp.Value.Category)
                                             .OrderBy(group => group.Key);

            foreach (var categoryGroup in categorizedTypes)
            {
                string category = categoryGroup.Key;
                
                if (!_categoryFoldouts.ContainsKey(category))
                {
                    _categoryFoldouts[category] = true;
                }

                _categoryFoldouts[category] = EditorGUILayout.Foldout(_categoryFoldouts[category], 
                    $"{category} Configurations", true);

                if (_categoryFoldouts[category])
                {
                    EditorGUI.indentLevel++;

                    foreach (var configEntry in categoryGroup)
                    {
                        DrawConfigurationEntry(configEntry.Key, configEntry.Value);
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }
        }

        private void DrawConfigurationEntry(System.Type configType, FluxConfigurationAttribute attribute)
        {
            EditorGUILayout.BeginVertical("box");

            // Header
            EditorGUILayout.LabelField(attribute.DisplayName ?? configType.Name, EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(attribute.Description))
            {
                EditorGUILayout.LabelField(attribute.Description, EditorStyles.wordWrappedLabel);
            }

            // Configuration status
            var loadedConfig = FluxConfigurationManager.GetConfiguration(configType);
            bool isLoaded = loadedConfig != null;

            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
            EditorGUILayout.LabelField(isLoaded ? "Loaded" : "Not Loaded", 
                isLoaded ? EditorStyles.label : EditorStyles.boldLabel);

            if (attribute.IsRequired)
            {
                EditorGUILayout.LabelField("(Required)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            // Properties
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Load Priority: {attribute.LoadPriority}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Auto Create: {attribute.AutoCreate}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Actions
            EditorGUILayout.BeginHorizontal();

            if (isLoaded)
            {
                if (GUILayout.Button("Select Asset", GUILayout.Width(100)))
                {
                    Selection.activeObject = loadedConfig;
                    EditorGUIUtility.PingObject(loadedConfig);
                }

                if (GUILayout.Button("Validate", GUILayout.Width(100)))
                {
                    bool isValid = loadedConfig.ValidateConfiguration();
                    EditorUtility.DisplayDialog("Validation", 
                        isValid ? "Configuration is valid" : "Configuration has errors. Check console.", 
                        "OK");
                }

                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("Apply (Play Mode)"))
                {
                    var manager = FluxManager.Instance;
                    if (manager != null)
                    {
                        loadedConfig.ApplyConfiguration(manager);
                        EditorUtility.DisplayDialog("Applied", "Configuration applied successfully.", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Could not apply configuration because FluxManager is not active in the scene.", "OK");
                    }
                }
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("Create Asset", GUILayout.Width(100)))
                {
                    CreateConfigurationAsset(configType, attribute);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateConfigurationAsset(System.Type configType, FluxConfigurationAttribute attribute)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Configuration Asset",
                configType.Name,
                "asset",
                "Choose where to save the configuration asset");

            if (!string.IsNullOrEmpty(path))
            {
                var asset = ScriptableObject.CreateInstance(configType);
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);

                FluxConfigurationManager.RegisterConfiguration(asset as FluxConfigurationAsset);
                FluxConfigurationManager.Initialize(); // Re-initialize to load the new asset
            }
        }
    }
}
