using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for FluxValidation attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxValidationAttribute))]
    public class FluxValidationDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, string> _validationErrorCache = new Dictionary<string, string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string propertyPath = property.propertyPath;
            bool isValid = true;
            string errorMessage = "";

            // Only re-validate if the property has changed
            if (GUI.changed)
            {
                isValid = ValidateProperty(property, out errorMessage);
                _validationErrorCache[propertyPath] = errorMessage;
            }
            else if (_validationErrorCache.TryGetValue(propertyPath, out string cachedError))
            {
                errorMessage = cachedError;
                isValid = string.IsNullOrEmpty(errorMessage);
            }
            else
            {
                isValid = ValidateProperty(property, out errorMessage);
                _validationErrorCache[propertyPath] = errorMessage;
            }

            // Change color based on validation result
            var originalColor = GUI.backgroundColor;
            if (!isValid)
            {
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f, 1f); // Light red for invalid
            }

            EditorGUI.PropertyField(position, property, label, true);

            GUI.backgroundColor = originalColor;

            // Show validation message if invalid
            if (!isValid && !string.IsNullOrEmpty(errorMessage))
            {
                var helpBoxPosition = new Rect(position.x, position.y + EditorGUI.GetPropertyHeight(property, label), position.width, EditorGUIUtility.singleLineHeight * 1.5f);
                EditorGUI.HelpBox(helpBoxPosition, errorMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Validates the property by retrieving its value and using the central FluxValidator.
        /// </summary>
        private bool ValidateProperty(SerializedProperty property, out string errorMessage)
        {
            // Get the actual object and field info the property represents
            var targetObject = property.serializedObject.targetObject;
            var fieldInfo = GetFieldInfoFromProperty(property);

            if (fieldInfo == null)
            {
                errorMessage = "Could not find field info for validation.";
                return true; // Cannot validate, so we assume it's fine
            }

            // Get the actual current value of the property
            object propertyValue = GetValueFromProperty(property);

            return FluxValidator.ValidateValue(fieldInfo, propertyValue, out errorMessage);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);

            // Check cache for error state
            if (_validationErrorCache.TryGetValue(property.propertyPath, out string errorMessage) && !string.IsNullOrEmpty(errorMessage))
            {
                return baseHeight + (EditorGUIUtility.singleLineHeight * 1.5f) + 2f; // Add space for help box
            }

            return baseHeight;
        }
        
        #region Reflection Helpers

        /// <summary>
        /// Gets the object value from a SerializedProperty.
        /// </summary>
        private object GetValueFromProperty(SerializedProperty property)
        {
            // This is a simplified version. A full version would handle nested objects.
            var targetObject = property.serializedObject.targetObject;
            var fieldInfo = GetFieldInfoFromProperty(property);
            return fieldInfo?.GetValue(targetObject);
        }

        /// <summary>
        /// Gets the FieldInfo for a SerializedProperty using reflection.
        /// Handles nested properties and arrays/lists.
        /// </summary>
        private FieldInfo GetFieldInfoFromProperty(SerializedProperty property)
        {
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] parts = path.Split('.');
            object currentObject = property.serializedObject.targetObject;
            FieldInfo fi = null;

            foreach (var part in parts)
            {
                if (currentObject == null) return null;

                if (part.Contains("["))
                {
                    string arrayName = part.Substring(0, part.IndexOf("["));
                    int index = Convert.ToInt32(part.Substring(part.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    fi = currentObject.GetType().GetField(arrayName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (fi == null) return null;
                    
                    var array = fi.GetValue(currentObject) as IEnumerable;
                    if (array == null) return null;
                    
                    var enumerator = array.GetEnumerator();
                    for (int i = 0; i <= index; i++)
                    {
                        if (!enumerator.MoveNext()) return null;
                    }
                    currentObject = enumerator.Current;
                }
                else
                {
                    fi = currentObject.GetType().GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi == null) return null;
                    currentObject = fi.GetValue(currentObject);
                }
            }
            return fi;
        }

        #endregion
    }
}
