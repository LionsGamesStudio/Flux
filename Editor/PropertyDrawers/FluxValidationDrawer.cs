using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;
using FluxFramework.Core;
using System.Linq;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A generic PropertyDrawer that works with any attribute inheriting from FluxValidationAttribute.
    /// It dynamically discovers and applies validators in the Unity Inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxValidationAttribute), true)]
    public class FluxValidationDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, string> _validationErrorCache = new Dictionary<string, string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string propertyPath = property.propertyPath;
            bool isValid;
            string errorMessage;

            if (GUI.changed || !_validationErrorCache.ContainsKey(propertyPath))
            {
                isValid = ValidateProperty(property, out errorMessage);
                _validationErrorCache[propertyPath] = errorMessage;
            }
            else
            {
                errorMessage = _validationErrorCache[propertyPath];
                isValid = string.IsNullOrEmpty(errorMessage);
            }
            
            var originalColor = GUI.backgroundColor;
            if (!isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f, 1f); // Light red for invalid
            }

            EditorGUI.PropertyField(position, property, label, true);

            GUI.backgroundColor = originalColor;

            if (!isValid)
            {
                float propertyHeight = EditorGUI.GetPropertyHeight(property, label);
                Rect helpBoxPosition = new Rect(position.x, position.y + propertyHeight, position.width, GetHelpBoxHeight(errorMessage));
                EditorGUI.HelpBox(helpBoxPosition, errorMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Validates the property using the new, decoupled architecture.
        /// </summary>
        private bool ValidateProperty(SerializedProperty property, out string errorMessage)
        {
            var fieldInfo = GetFieldInfoFromProperty(property);
            if (fieldInfo == null)
            {
                errorMessage = "Internal Error: Could not find FieldInfo for validation.";
                return true;
            }
            
            object propertyValue = GetValueFromProperty(property);

            var validationAttributes = fieldInfo.GetCustomAttributes<FluxValidationAttribute>(true);
            var errorMessages = new List<string>();

            foreach (var attr in validationAttributes)
            {
                IValidator validator = attr.CreateValidator(fieldInfo);
                if (validator != null)
                {
                    try
                    {
                        MethodInfo validateMethod = validator.GetType().GetMethod("Validate");
                        ValidationResult result = (ValidationResult)validateMethod.Invoke(validator, new[] { propertyValue });

                        if (!result.IsValid)
                        {
                            errorMessages.AddRange(result.ErrorMessages);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FluxValidationDrawer] Error while executing validator '{validator.GetType().Name}': {ex.Message}");
                    }
                }
            }
            
            if (errorMessages.Any())
            {
                errorMessage = string.Join("\n", errorMessages);
                return false;
            }

            errorMessage = "";
            return true;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            
            if (_validationErrorCache.TryGetValue(property.propertyPath, out string errorMessage) && !string.IsNullOrEmpty(errorMessage))
            {
                return baseHeight + GetHelpBoxHeight(errorMessage) + 2f;
            }

            return baseHeight;
        }

        private float GetHelpBoxHeight(string message)
        {
            var content = new GUIContent(message);
            var style = new GUIStyle(EditorStyles.helpBox);
            // The default helpbox style might not wrap text, so ensure it does for height calculation
            style.wordWrap = true;
            float height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth - 40); // Subtract some padding
            return Mathf.Max(EditorGUIUtility.singleLineHeight * 1.5f, height);
        }

        #region Reflection Helpers

        /// <summary>
        /// Gets the actual object value from a SerializedProperty using reflection.
        /// Handles nested properties.
        /// </summary>
        private object GetValueFromProperty(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;
            
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    targetObject = GetValue(targetObject, elementName, index);
                }
                else
                {
                    targetObject = GetValue(targetObject, element);
                }
            }
            return targetObject;
        }

        /// <summary>
        /// A generic reflection helper to get a value from a field or property.
        /// </summary>
        private object GetValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(source);
            }

            var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null)
            {
                return property.GetValue(source, null);
            }
            return null;
        }

        /// <summary>
        /// A helper to get a value from an enumerable (array/list) at a specific index.
        /// </summary>
        private object GetValue(object source, string name, int index)
        {
            if (!(GetValue(source, name) is IEnumerable enumerable)) return null;
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }
            return enumerator.Current;
        }

        /// <summary>
        /// A robust helper to get the FieldInfo for a property, correctly handling nested fields.
        /// </summary>
        private FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            var propertyPath = property.propertyPath;
            
            // Handle arrays/lists. e.g., "myList.Array.data[0]" should return the FieldInfo for "myList".
            if (propertyPath.Contains(".Array.data["))
            {
                propertyPath = propertyPath.Substring(0, propertyPath.IndexOf(".Array.data["));
            }

            // Handle nested fields. e.g., "playerStats.health"
            var pathParts = propertyPath.Split('.');
            FieldInfo fieldInfo = null;
            Type currentType = targetType;

            foreach (var part in pathParts)
            {
                fieldInfo = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo == null) return null; // Path is invalid
                currentType = fieldInfo.FieldType; // Traverse down for the next part of the path
            }

            return fieldInfo;
        }
        #endregion
    }
}