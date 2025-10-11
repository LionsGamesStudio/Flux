using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FluxFramework.Configuration;

namespace FluxFramework.Editor
{
    /// <summary>
    /// An editor window that displays a searchable list of all property keys
    /// defined in the project's FluxPropertyDefinitions assets.
    /// It allows developers to easily find and copy keys to avoid typos.
    /// </summary>
    public class PropertyKeyViewerWindow : EditorWindow
    {
        private List<PropertyDefinition> _allDefinitions;
        private string _searchText = "";
        private Vector2 _scrollPosition;

        public static void ShowWindow()
        {
            // GetWindow will focus the existing window or create a new one.
            GetWindow<PropertyKeyViewerWindow>("Property Keys");
        }

        /// <summary>
        /// Called when the window is enabled. Loads the property key definitions.
        /// </summary>
        private void OnEnable()
        {
            LoadKeys();
        }

        private void LoadKeys()
        {
            _allDefinitions = new List<PropertyDefinition>();
            
            // Find all assets of type FluxPropertyDefinitions in the entire project.
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FluxPropertyDefinitions)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definitionsAsset = AssetDatabase.LoadAssetAtPath<FluxPropertyDefinitions>(path);
                if (definitionsAsset != null)
                {
                    _allDefinitions.AddRange(definitionsAsset.properties);
                }
            }
            
            // Sort the list alphabetically for better readability.
            _allDefinitions = _allDefinitions.OrderBy(d => d.key).ToList();
        }

        private void OnGUI()
        {
            // --- Search Bar ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadKeys(); // Reload all definitions from the project
            }
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Filter the definitions based on the search text.
            var filteredDefinitions = _allDefinitions
                .Where(def => string.IsNullOrEmpty(_searchText) || 
                              def.key.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()) ||
                              def.description.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()))
                .ToList();

            // --- List of Properties ---
            foreach (var def in filteredDefinitions)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // Display key, description, and type.
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(def.key, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(def.description))
                {
                    EditorGUILayout.LabelField(def.description, EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField(def.type.ToString(), GUILayout.Width(60));
                
                // The "Copy" button.
                if (GUILayout.Button("Copy Key", GUILayout.Width(80)))
                {
                    // Copy the key to the system clipboard.
                    GUIUtility.systemCopyBuffer = def.key;
                    FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Copied to clipboard: {def.key}");
                    // Optionally, show a temporary notification on the window itself.
                    this.ShowNotification(new GUIContent("Key Copied!"));
                }
                
                EditorGUILayout.EndHorizontal();
            }

            if (!filteredDefinitions.Any())
            {
                EditorGUILayout.HelpBox("No property keys found or none match your search.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}