#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluxFramework.UI;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Custom inspector for the FluxMonoBehaviour base class and its children.
    /// Provides features like Groups and Buttons.
    /// </summary>
    [CustomEditor(typeof(FluxMonoBehaviour), true)]
    public class FluxComponentEditor : UnityEditor.Editor
    {
        private Dictionary<string, List<SerializedProperty>> _groupedProperties;
        private List<SerializedProperty> _ungroupedProperties;
        private Dictionary<string, bool> _foldoutStates;
        private Dictionary<string, FluxGroupAttribute> _groupAttributes;
        private Dictionary<MethodInfo, object[]> _actionParameters = new Dictionary<MethodInfo, object[]>();

        private void OnEnable()
        {
            // Initialize dictionaries to store group information
            _groupedProperties = new Dictionary<string, List<SerializedProperty>>();
            _ungroupedProperties = new List<SerializedProperty>();
            _foldoutStates = new Dictionary<string, bool>();
            _groupAttributes = new Dictionary<string, FluxGroupAttribute>();

            // Use SerializedObject to iterate through all visible properties
            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    // Skip the default "m_Script" property
                    if (iterator.name == "m_Script") continue;
                    
                    var property = serializedObject.FindProperty(iterator.name);
                    var fieldInfo = GetFieldInfo(property);
                    
                    if (fieldInfo != null)
                    {
                        var groupAttr = fieldInfo.GetCustomAttribute<FluxGroupAttribute>();
                        if (groupAttr != null)
                        {
                            // This property belongs to a group
                            if (!_groupedProperties.ContainsKey(groupAttr.GroupName))
                            {
                                _groupedProperties[groupAttr.GroupName] = new List<SerializedProperty>();
                                _groupAttributes[groupAttr.GroupName] = groupAttr;
                                // Set initial foldout state
                                _foldoutStates[groupAttr.GroupName] = !groupAttr.StartCollapsed;
                            }
                            _groupedProperties[groupAttr.GroupName].Add(property.Copy());
                        }
                        else
                        {
                            // This property does not have a group
                            _ungroupedProperties.Add(property.Copy());
                        }
                    }
                }
                while (iterator.NextVisible(false));
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Always start with this

            // --- Draw Ungrouped Properties First ---
            foreach (var prop in _ungroupedProperties)
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            // --- Draw Grouped Properties ---
            // Order groups by their 'Order' property
            var orderedGroups = _groupedProperties.Keys.OrderBy(key => _groupAttributes[key].Order).ToList();

            foreach (var groupName in orderedGroups)
            {
                var groupProperties = _groupedProperties[groupName];
                var groupAttribute = _groupAttributes[groupName];

                EditorGUILayout.Space();
                
                // Draw the foldout header for the group
                _foldoutStates[groupName] = EditorGUILayout.Foldout(_foldoutStates[groupName], groupName, true, EditorStyles.foldoutHeader);

                if (_foldoutStates[groupName])
                {
                    EditorGUI.indentLevel++;
                    foreach (var prop in groupProperties)
                    {
                        EditorGUILayout.PropertyField(prop, true);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            DrawButtons();

            EditorGUILayout.Space();
            DrawActions();

            serializedObject.ApplyModifiedProperties(); // Always end with this
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

        private void DrawActions()
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<FluxActionAttribute>() != null)
                .ToList();

            if (methods.Any())
            {
                EditorGUILayout.LabelField("Debug Actions", EditorStyles.boldLabel);
                foreach (var method in methods)
                {
                    var actionAttr = method.GetCustomAttribute<FluxActionAttribute>();
                    var parameters = method.GetParameters();

                    if (!_actionParameters.ContainsKey(method))
                    {
                        _actionParameters[method] = new object[parameters.Length];
                    }

                    // Draw a box around the action for better visual separation
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    string displayName = string.IsNullOrEmpty(actionAttr.DisplayName) ? ObjectNames.NicifyVariableName(method.Name) : actionAttr.DisplayName;
                    EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel++;

                    bool allParamsSupported = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var paramValue = _actionParameters[method][i];
                        
                        // Draw a field based on the parameter type
                        if (param.ParameterType == typeof(int))
                            _actionParameters[method][i] = EditorGUILayout.IntField(param.Name, (int)(paramValue ?? 0));
                        else if (param.ParameterType == typeof(float))
                            _actionParameters[method][i] = EditorGUILayout.FloatField(param.Name, (float)(paramValue ?? 0f));
                        else if (param.ParameterType == typeof(string))
                            _actionParameters[method][i] = EditorGUILayout.TextField(param.Name, (string)paramValue);
                        else if (param.ParameterType == typeof(bool))
                            _actionParameters[method][i] = EditorGUILayout.Toggle(param.Name, (bool)(paramValue ?? false));
                        else if (typeof(UnityEngine.Object).IsAssignableFrom(param.ParameterType))
                            _actionParameters[method][i] = EditorGUILayout.ObjectField(param.Name, (UnityEngine.Object)paramValue, param.ParameterType, true);
                        else
                        {
                            EditorGUILayout.LabelField(param.Name, $"Unsupported type: {param.ParameterType.Name}");
                            allParamsSupported = false;
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                    
                    // The button is only enabled if all parameters are of a supported type.
                    using (new EditorGUI.DisabledScope(!allParamsSupported))
                    {
                        if (GUILayout.Button(actionAttr.ButtonText))
                        {
                            method.Invoke(target, _actionParameters[method]);
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }
        
        // Helper to get FieldInfo from a SerializedProperty
        private FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            return targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
#endif

  