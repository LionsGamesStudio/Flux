using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting.Nodes;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Editor.Inspectors
{
    /// <summary>
    /// Custom editor for EventPublishNode. It uses SerializedProperty for robust data handling
    /// and TypeCache for efficient event type discovery.
    /// </summary>
    [CustomEditor(typeof(EventPublishNode))]
    public class EventPublishNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _eventTypeProp;
        private SerializedProperty _customDisplayNameProp;
        
        private List<string> _availableEventTypes;
        private int _selectedEventIndex = 0;
        private bool _useManualEntry = false;

        private void OnEnable()
        {
            // Find properties using their private field names for robustness.
            _eventTypeProp = serializedObject.FindProperty("_eventType");
            _customDisplayNameProp = serializedObject.FindProperty("_customDisplayName");
            
            RefreshEventTypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Publisher Node", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_customDisplayNameProp, new GUIContent("Custom Name"));
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Event Type", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshEventTypes();
            }
            EditorGUILayout.EndHorizontal();

            _useManualEntry = EditorGUILayout.Toggle("Manual Entry", _useManualEntry);
            
            string newEventType;
            if (_useManualEntry)
            {
                newEventType = EditorGUILayout.TextField("Event Type", _eventTypeProp.stringValue);
            }
            else
            {
                if (_availableEventTypes != null && _availableEventTypes.Count > 1)
                {
                    _selectedEventIndex = _availableEventTypes.IndexOf(_eventTypeProp.stringValue);
                    if (_selectedEventIndex < 0) _selectedEventIndex = 0;

                    _selectedEventIndex = EditorGUILayout.Popup("Event Type", _selectedEventIndex, _availableEventTypes.ToArray());
                    newEventType = _availableEventTypes[_selectedEventIndex];
                }
                else
                {
                    EditorGUILayout.HelpBox("No event types discovered. Use 'Refresh' or enable 'Manual Entry'.", MessageType.Info);
                    newEventType = EditorGUILayout.TextField("Event Type", _eventTypeProp.stringValue);
                }
            }

            if (newEventType != _eventTypeProp.stringValue)
            {
                _eventTypeProp.stringValue = newEventType;
            }

            EditorGUILayout.Space();
            
            // --- Read the value directly from the SerializedProperty ---
            string currentEventTypeValue = _eventTypeProp.stringValue;

            EditorGUILayout.LabelField("Event Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Event Type", string.IsNullOrEmpty(currentEventTypeValue) ? "Generic Event" : currentEventTypeValue);
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(currentEventTypeValue))
            {
                EditorGUILayout.HelpBox($"Will publish a '{currentEventTypeValue}' event when executed.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No specific event type specified. A GenericFluxEvent will be published.", MessageType.Info);
            }

            // Apply all changes made to SerializedProperties.
            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshEventTypes()
        {
            try
            {
                // Use TypeCache for robust, efficient discovery of all classes implementing IFluxEvent.
                _availableEventTypes = TypeCache.GetTypesDerivedFrom<IFluxEvent>()
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && t != typeof(GenericFluxEvent)) // Exclude the generic one from the list
                    .Select(t => t.FullName) // Use FullName to avoid ambiguity
                    .OrderBy(name => name)
                    .ToList();
                
                // Add an option for publishing a GenericFluxEvent (represented by an empty string).
                _availableEventTypes.Insert(0, ""); // Represents "Generic Event"
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to discover event types: {ex.Message}");
                _availableEventTypes = new List<string> { "" };
            }
        }
    }
}