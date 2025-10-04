using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using FluxFramework.Core;
using FluxFramework.Events;
using System.Linq;

namespace FluxFramework.Editor
{
    public class ReactivePropertyInspectorWindow : EditorWindow
    {
        private Dictionary<string, IReactiveProperty> _properties = new Dictionary<string, IReactiveProperty>();
        private Vector2 _scrollPosition;
        private string _searchText = "";
        
        private IDisposable _propertyChangeEventSubscription;

        public static void ShowWindow()
        {
            GetWindow<ReactivePropertyInspectorWindow>("Reactive Properties");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            if (Application.isPlaying)
            {
                SubscribeToEvents();
                FetchAllProperties();
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnsubscribeFromEvents();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SubscribeToEvents();
                FetchAllProperties();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                UnsubscribeFromEvents();
                _properties.Clear();
                Repaint();
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_propertyChangeEventSubscription != null || Flux.Manager == null) return;

            _propertyChangeEventSubscription = Flux.Manager.EventBus.Subscribe<PropertyChangedEvent>(OnPropertyChanged);
        }

        private void UnsubscribeFromEvents()
        {
            _propertyChangeEventSubscription?.Dispose();
            _propertyChangeEventSubscription = null;
        }

        // Cette méthode est appelée LORSQU'UNE PROPRIÉTÉ CHANGE
        private void OnPropertyChanged(PropertyChangedEvent evt)
        {
            Repaint();
        }

        private void FetchAllProperties()
        {
            if (!Application.isPlaying || Flux.Manager == null) return;
            
            var currentKeys = Flux.Manager.Properties.GetAllPropertyKeys();
            _properties = currentKeys.ToDictionary(key => key, key => Flux.Manager.Properties.GetProperty(key));
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Reactive Properties Inspector", EditorStyles.boldLabel);
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view and edit reactive properties.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_properties == null || _properties.Count == 0)
            {
                EditorGUILayout.LabelField("No reactive properties registered.");
            }
            else
            {
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

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var collection = currentValue as System.Collections.IEnumerable;
                if (collection != null)
                {
                    int count = 0;
                    var preview = new System.Text.StringBuilder();

                    foreach (var item in collection)
                    {
                        if (count < 3)
                        {
                            preview.Append(item?.ToString() ?? "null");
                            preview.Append(", ");
                        }
                        count++;
                    }
                    if (preview.Length > 2) preview.Length -= 2;
                    if (count > 3) preview.Append("...");

                    EditorGUILayout.LabelField(new GUIContent($"Count: {count}", preview.ToString()), EditorStyles.helpBox);
                }
                else
                {
                    EditorGUILayout.LabelField("null collection");
                }
            }
            else
            {
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