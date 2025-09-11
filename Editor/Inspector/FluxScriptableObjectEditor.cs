    
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A powerful custom inspector for all classes inheriting from FluxScriptableObject.
    /// It automatically handles drawing fields in groups ([FluxGroup]) and rendering methods
    /// as buttons ([FluxButton]).
    /// </summary>
    [CustomEditor(typeof(FluxScriptableObject), true)]
    public class FluxScriptableObjectEditor : UnityEditor.Editor
    {
        private Dictionary<string, List<SerializedProperty>> _groupedProperties;
        private List<SerializedProperty> _ungroupedProperties;
        private Dictionary<string, bool> _foldoutStates;
        private Dictionary<string, FluxGroupAttribute> _groupAttributes;

        private void OnEnable()
        {
            _groupedProperties = new Dictionary<string, List<SerializedProperty>>();
            _ungroupedProperties = new List<SerializedProperty>();
            _foldoutStates = new Dictionary<string, bool>();
            _groupAttributes = new Dictionary<string, FluxGroupAttribute>();

            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_Script") continue;
                    
                    var property = serializedObject.FindProperty(iterator.name);
                    var fieldInfo = GetFieldInfo(property);
                    
                    if (fieldInfo != null)
                    {
                        var groupAttr = fieldInfo.GetCustomAttribute<FluxGroupAttribute>();
                        if (groupAttr != null)
                        {
                            if (!_groupedProperties.ContainsKey(groupAttr.GroupName))
                            {
                                _groupedProperties[groupAttr.GroupName] = new List<SerializedProperty>();
                                _groupAttributes[groupAttr.GroupName] = groupAttr;
                                _foldoutStates[groupAttr.GroupName] = !groupAttr.StartCollapsed;
                            }
                            _groupedProperties[groupAttr.GroupName].Add(property.Copy());
                        }
                        else
                        {
                            _ungroupedProperties.Add(property.Copy());
                        }
                    }
                }
                while (iterator.NextVisible(false));
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw ungrouped properties first.
            foreach (var prop in _ungroupedProperties)
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            // Draw grouped properties in their foldouts.
            var orderedGroups = _groupedProperties.Keys.OrderBy(key => _groupAttributes[key].Order).ToList();
            foreach (var groupName in orderedGroups)
            {
                EditorGUILayout.Space(5);
                _foldoutStates[groupName] = EditorGUILayout.Foldout(_foldoutStates[groupName], groupName, true, EditorStyles.foldoutHeader);

                if (_foldoutStates[groupName])
                {
                    EditorGUI.indentLevel++;
                    foreach (var prop in _groupedProperties[groupName])
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            DrawButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawButtons()
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<FluxButtonAttribute>() != null)
                .ToList();

            if (methods.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                foreach (var method in methods)
                {
                    var buttonAttr = method.GetCustomAttribute<FluxButtonAttribute>();
                    
                    // --- Parameter Count Check ---
                    bool hasParameters = method.GetParameters().Length > 0;

                    // Determine if the button should be enabled based on play mode and parameters.
                    bool isEnabledInMode = Application.isPlaying ? buttonAttr.EnabledInPlayMode : buttonAttr.EnabledInEditMode;
                    
                    // A button is only truly clickable if it's enabled in the current mode AND has no parameters.
                    bool isClickable = isEnabledInMode && !hasParameters;
                    
                    // Use a disabled scope to show the button as grayed out if it's not clickable.
                    using (new EditorGUI.DisabledScope(!isClickable))
                    {
                        string buttonText = string.IsNullOrEmpty(buttonAttr.ButtonText) ? method.Name : buttonAttr.ButtonText;

                        if (GUILayout.Button(buttonText))
                        {
                            // This code will now only be reached if the button is clickable.
                            method.Invoke(target, null);
                        }
                    }

                    // If the button is disabled because it has parameters, show a helpful warning message.
                    if (hasParameters)
                    {
                        EditorGUILayout.HelpBox($"Method '{method.Name}' has parameters and cannot be used as a FluxButton.", MessageType.Warning);
                    }
                }
            }
        }
        
        // Helper to get FieldInfo from a SerializedProperty
        private FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            // This simplified version works for top-level fields.
            // A more complex version would handle nested properties if needed.
            return targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
#endif