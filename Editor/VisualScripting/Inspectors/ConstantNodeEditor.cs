using System;
using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor.Inspectors
{
    /// <summary>
    /// Custom editor for ConstantNode that provides a dynamic value field based on the selected type.
    /// It uses SerializedProperty for robust data handling.
    /// </summary>
    [CustomEditor(typeof(ConstantNode))]
    public class ConstantNodeEditor : UnityEditor.Editor
    {
        // We only need references to the SerializedProperties.
        private SerializedProperty _constantTypeProp;
        private SerializedProperty _floatValueProp;
        private SerializedProperty _intValueProp;
        private SerializedProperty _boolValueProp;
        private SerializedProperty _stringValueProp;
        private SerializedProperty _vector2ValueProp;
        private SerializedProperty _vector3ValueProp;
        private SerializedProperty _customDisplayNameProp;

        private void OnEnable()
        {
            // Find properties by their private field name (_fieldName).
            _constantTypeProp = serializedObject.FindProperty("_constantType");
            _floatValueProp = serializedObject.FindProperty("_floatValue");
            _intValueProp = serializedObject.FindProperty("_intValue");
            _boolValueProp = serializedObject.FindProperty("_boolValue");
            _stringValueProp = serializedObject.FindProperty("_stringValue");
            _vector2ValueProp = serializedObject.FindProperty("_vector2Value");
            _vector3ValueProp = serializedObject.FindProperty("_vector3Value");
            _customDisplayNameProp = serializedObject.FindProperty("_customDisplayName");
        }

        public override void OnInspectorGUI()
        {
            // Always start with Update() to get the latest data from the target object.
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Constant Node", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_customDisplayNameProp, new GUIContent("Custom Name"));
            EditorGUILayout.Space();

            // --- REFACTORED TYPE DROPDOWN ---
            EditorGUI.BeginChangeCheck();
            // Draw the enum dropdown directly from the SerializedProperty.
            EditorGUILayout.PropertyField(_constantTypeProp, new GUIContent("Type"));
            if (EditorGUI.EndChangeCheck())
            {
                // When the type changes, we need to apply the change so the switch statement below
                // reads the new value for the current frame.
                serializedObject.ApplyModifiedProperties();
                
                // We still need to notify the node to refresh its ports.
                // It's safe to keep a cast to the target for calling methods.
                var node = (ConstantNode)target;
                node.RefreshPorts(); // This method should exist on the node.
            }

            EditorGUILayout.Space();
            
            // Get the current enum type for the switch statement.
            var currentType = (ConstantType)_constantTypeProp.enumValueIndex;

            // Display the correct value field based on the selected type.
            switch (currentType)
            {
                case ConstantType.Float:
                    EditorGUILayout.PropertyField(_floatValueProp, new GUIContent("Value"));
                    break;
                case ConstantType.Int:
                    EditorGUILayout.PropertyField(_intValueProp, new GUIContent("Value"));
                    break;
                case ConstantType.Bool:
                    EditorGUILayout.PropertyField(_boolValueProp, new GUIContent("Value"));
                    break;
                case ConstantType.String:
                    EditorGUILayout.PropertyField(_stringValueProp, new GUIContent("Value"));
                    break;
                case ConstantType.Vector2:
                    EditorGUILayout.PropertyField(_vector2ValueProp, new GUIContent("Value"));
                    break;
                case ConstantType.Vector3:
                    EditorGUILayout.PropertyField(_vector3ValueProp, new GUIContent("Value"));
                    break;
            }

            EditorGUILayout.Space();

            // Show current output value info (read-only).
            EditorGUILayout.LabelField("Output Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Output Type", currentType.ToString());
            EditorGUILayout.TextField("Current Value", GetCurrentValueAsString(currentType));
            EditorGUI.EndDisabledGroup();

            // Always end with ApplyModifiedProperties() to save any changes made.
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Reads the current value directly from the SerializedProperty for display.
        /// </summary>
        private string GetCurrentValueAsString(ConstantType type)
        {
            // It's safer to read from the SerializedProperty as it reflects the current editor state.
            switch (type)
            {
                case ConstantType.Float:
                    return _floatValueProp.floatValue.ToString("F3");
                case ConstantType.Int:
                    return _intValueProp.intValue.ToString();
                case ConstantType.Bool:
                    return _boolValueProp.boolValue.ToString();
                case ConstantType.String:
                    return $"\"{_stringValueProp.stringValue}\"";
                case ConstantType.Vector2:
                    return _vector2ValueProp.vector2Value.ToString();
                case ConstantType.Vector3:
                    return _vector3ValueProp.vector3Value.ToString();
                default:
                    return "N/A";
            }
        }
    }
}