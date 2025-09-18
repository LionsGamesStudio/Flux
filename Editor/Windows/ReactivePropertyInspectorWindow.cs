using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using FluxFramework.Core;
using System.Linq;

namespace FluxFramework.Editor
{
    public class ReactivePropertyInspectorWindow : EditorWindow
    {
        private Dictionary<string, IReactiveProperty> _properties = new Dictionary<string, IReactiveProperty>();
        private Vector2 _scrollPosition;
        private string _searchText = "";

        public static void ShowWindow()
        {
            GetWindow<ReactivePropertyInspectorWindow>("Reactive Properties");
        }

        private void OnEnable()
        {
            // Subscribe to the editor update loop to refresh the property list and values
            EditorApplication.update += RefreshData;
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            EditorApplication.update -= RefreshData;
        }

        private void RefreshData()
        {
            // We only need to fetch data if the application is playing
            if (!Application.isPlaying || Flux.Manager == null)
            {
                if (_properties.Count > 0)
                {
                    _properties.Clear();
                    Repaint(); // Clear the view when exiting play mode
                }
                return;
            }

            // Get the current list of properties from the manager
            var currentKeys = Flux.Manager.Properties.GetAllPropertyKeys();
            _properties = currentKeys.ToDictionary(key => key, key => Flux.Manager.Properties.GetProperty(key));

            Repaint(); // Redraw the window with the latest values
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Reactive Properties Inspector", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view and edit reactive properties.", MessageType.Info);
                return;
            }

            // --- Toolbar ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            // --- Property List ---
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_properties == null || _properties.Count == 0)
            {
                EditorGUILayout.LabelField("No reactive properties registered.");
            }
            else
            {
                // Create a sorted list of keys for a consistent display order
                var filteredKeys = _properties.Keys
                    .Where(k => string.IsNullOrEmpty(_searchText) || k.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()))
                    .OrderBy(k => k)
                    .ToList();

                foreach (var key in filteredKeys)
                {
                    if (_properties.TryGetValue(key, out IReactiveProperty property) && property != null)
                    {
                        DrawPropertyEditor(key, property);
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the correct editor field for a property based on its ValueType.
        /// </summary>
        private void DrawPropertyEditor(string key, IReactiveProperty property)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Display the property key
            EditorGUILayout.LabelField(new GUIContent(key, property.ValueType.Name), GUILayout.MinWidth(100), GUILayout.MaxWidth(300));
            
            object currentValue = property.GetValue();
            Type type = property.ValueType;

            EditorGUI.BeginChangeCheck();
            object newValue = currentValue;

            // --- Type-Specific Editor Fields ---
            if (type == typeof(int))
            {
                newValue = EditorGUILayout.IntField((int)currentValue);
            }
            else if (type == typeof(float))
            {
                newValue = EditorGUILayout.FloatField((float)currentValue);
            }
            else if (type == typeof(string))
            {
                newValue = EditorGUILayout.TextField((string)currentValue);
            }
            else if (type == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle((bool)currentValue);
            }
            else if (type == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField((Color)currentValue);
            }
            else if (type == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field("", (Vector2)currentValue);
            }
            else if (type == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field("", (Vector3)currentValue);
            }
            else if (type.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup((Enum)currentValue);
            }
            else
            {
                // For unsupported types, just display the value as a read-only string
                EditorGUILayout.LabelField(currentValue?.ToString() ?? "null");
            }

            if (EditorGUI.EndChangeCheck())
            {
                // If the user changed the value in the GUI, update the property in the game.
                property.SetValue(newValue);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}