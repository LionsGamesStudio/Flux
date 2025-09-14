using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting.Nodes;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Editor.Inspectors
{
    /// <summary>
    /// Custom editor for EventListenerNode. It uses SerializedProperty for robust data handling
    /// and TypeCache for efficient event type discovery.
    /// </summary>
    [CustomEditor(typeof(EventListenerNode))]
    public class EventListenerNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _eventNameProp;
        private SerializedProperty _customDisplayNameProp;
        
        private List<string> _availableEventTypes;
        private int _selectedEventIndex = 0;
        private bool _useManualEntry = false;

        private void OnEnable()
        {
            _eventNameProp = serializedObject.FindProperty("_eventName");
            _customDisplayNameProp = serializedObject.FindProperty("_customDisplayName");
            
            RefreshEventTypes();
        }

        public override void OnInspectorGUI()
        {
            if (_eventNameProp == null || _customDisplayNameProp == null)
            {
                EditorGUILayout.HelpBox("Could not find the required serialized properties ('_eventName', '_customDisplayName'). Has the node script been changed?", MessageType.Error);
                return;
            }

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Listener Node", EditorStyles.boldLabel);
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
                newEventType = EditorGUILayout.TextField("Event Type", _eventNameProp.stringValue);
            }
            else
            {
                if (_availableEventTypes != null && _availableEventTypes.Count > 1)
                {
                    _selectedEventIndex = _availableEventTypes.IndexOf(_eventNameProp.stringValue);
                    if (_selectedEventIndex < 0) _selectedEventIndex = 0;

                    _selectedEventIndex = EditorGUILayout.Popup("Event Type", _selectedEventIndex, _availableEventTypes.ToArray());
                    newEventType = _availableEventTypes[_selectedEventIndex];
                }
                else
                {
                    EditorGUILayout.HelpBox("No event types discovered. Use 'Refresh' or enable 'Manual Entry'.", MessageType.Info);
                    newEventType = EditorGUILayout.TextField("Event Type", _eventNameProp.stringValue);
                }
            }

            if (newEventType != _eventNameProp.stringValue)
            {
                _eventNameProp.stringValue = newEventType;
            }

            EditorGUILayout.Space();

            string currentEventTypeValue = _eventNameProp.stringValue;

            EditorGUILayout.LabelField("Event Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Event Type", string.IsNullOrEmpty(currentEventTypeValue) ? "None" : currentEventTypeValue);
            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(currentEventTypeValue))
            {
                EditorGUILayout.HelpBox($"Will listen for '{currentEventTypeValue}' events when playing.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshEventTypes()
        {
            try
            {
                var eventTypes = TypeCache.GetTypesDerivedFrom<IFluxEvent>();
                
                _availableEventTypes = eventTypes
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .Select(t => t.FullName)
                    .OrderBy(name => name)
                    .ToList();
                
                _availableEventTypes.Insert(0, ""); // Add a "None" option
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to discover event types: {ex.Message}");
                _availableEventTypes = new List<string> { "" };
            }
        }
    }
}