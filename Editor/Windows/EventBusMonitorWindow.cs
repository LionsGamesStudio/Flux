using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using FluxFramework.Core;
using FluxFramework.Editor.Utils;

namespace FluxFramework.Editor
{
    public class EventBusMonitorWindow : EditorWindow
    {
        private class EventLogEntry
        {
            public IFluxEvent Event;
            public string TypeName;
            public string Timestamp;
            public bool IsExpanded; // To show/hide details
        }

        private readonly List<EventLogEntry> _eventLog = new List<EventLogEntry>();
        private readonly Queue<EventLogEntry> _eventQueue = new Queue<EventLogEntry>();
        private const int MaxLogSize = 200; // Keep the list from growing indefinitely

        private Vector2 _scrollPosition;
        private bool _isPaused = false;
        private bool _isListening = false;
        private string _searchText = "";

        public static void ShowWindow()
        {
            GetWindow<EventBusMonitorWindow>("Event Bus Monitor");
        }

        private void OnEnable()
        {
            // Subscribe to the global hook when the window is opened or Unity recompiles
            StartListening();
            // This ensures we repaint the window frequently when in play mode to see new events
            EditorApplication.playModeStateChanged += HandlePlayModeState;
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks when the window is closed
            StopListening();
            EditorApplication.playModeStateChanged -= HandlePlayModeState;
        }

        private void Update()
        {
            // Process events from the queue in the main editor thread to avoid collection modification errors
            if (_eventQueue.Count > 0)
            {
                while (_eventQueue.Count > 0)
                {
                    var newEntry = _eventQueue.Dequeue();
                    _eventLog.Insert(0, newEntry); // Add to the top of the list
                }

                // Trim the log if it gets too long
                if (_eventLog.Count > MaxLogSize)
                {
                    _eventLog.RemoveRange(MaxLogSize, _eventLog.Count - MaxLogSize);
                }
                Repaint(); // Redraw the window
            }
        }

        private void OnGUI()
        {
            // --- Toolbar ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(_isListening ? "Stop Listening" : "Start Listening", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                if (_isListening) StopListening();
                else StartListening();
            }
            
            _isPaused = GUILayout.Toggle(_isPaused, "Pause", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                _eventLog.Clear();
                _eventQueue.Clear();
            }

            GUILayout.FlexibleSpace();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();

            // --- Event Log ---
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var filteredLog = _eventLog
                .Where(e => string.IsNullOrEmpty(_searchText) || e.TypeName.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()))
                .ToList();

            foreach (var entry in filteredLog)
            {
                DrawEventEntry(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEventEntry(EventLogEntry entry)
        {
            var eventTypeColor = GetColorForEventType(entry.TypeName);
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = eventTypeColor;

            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();
            entry.IsExpanded = EditorGUILayout.Foldout(entry.IsExpanded, "", true);
            EditorGUILayout.LabelField(entry.TypeName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(entry.Timestamp, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            // If expanded, show event details using reflection
            if (entry.IsExpanded)
            {
                EditorGUI.indentLevel++;
                var fields = entry.Event.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                var properties = entry.Event.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    // Exclude properties from the base class to avoid clutter
                    if (prop.DeclaringType == typeof(FluxEventBase) || prop.DeclaringType == typeof(object)) continue;
                    try
                    {
                        var value = prop.GetValue(entry.Event);
                        EditorGUILayout.LabelField(prop.Name, EditorDebugUtils.ToPrettyString(value));
                    }
                    catch { /* Ignore properties that can't be read */ }
                }
                
                foreach (var field in fields)
                {
                    EditorGUILayout.LabelField(field.Name, field.GetValue(entry.Event)?.ToString() ?? "null");
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void HandleEventPublished(IFluxEvent publishedEvent)
        {
            if (_isPaused || !_isListening) return;

            var entry = new EventLogEntry
            {
                Event = publishedEvent,
                TypeName = publishedEvent.GetType().Name,
                Timestamp = publishedEvent.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff")
            };

            // We can't modify the collection directly from another thread,
            // so we add to a thread-safe queue to be processed in Update()
            _eventQueue.Enqueue(entry);
        }

        private void StartListening()
        {
            if (_isListening || !Application.isPlaying || Flux.Manager == null) return;

            if (FluxEditorServices.EventBus != null)
            {
                FluxEditorServices.EventBus.OnEventPublished += HandleEventPublished;
                _isListening = true;
                Repaint();
            }
        }

        private void StopListening()
        {
            if (!_isListening) return;

            if (FluxEditorServices.EventBus != null)
            {
                FluxEditorServices.EventBus.OnEventPublished -= HandleEventPublished;
            }
            _isListening = false;
            Repaint();
        }

        private void HandlePlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _eventLog.Clear();
                _eventQueue.Clear();
                Repaint();
            }
        }
        
        private Color GetColorForEventType(string typeName)
        {
            // Simple hashing to get a consistent color for each event type
            int hash = typeName.GetHashCode();
            float h = (hash & 0xFF) / 255.0f;
            float s = 0.6f; // Keep saturation consistent
            float v = 0.9f; // Keep value/brightness consistent
            return Color.HSVToRGB(h, s, v);
        }
    }
}