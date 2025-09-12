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
    /// It dynamically discovers and applies validators in the Unity Inspector, providing immediate visual feedback.
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxValidationAttribute), true)]
    public class FluxValidationDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, string> _validationErrorCache = new Dictionary<string, string>();

        /// <summary>
        /// Renders the property field and its validation state in the Inspector.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string propertyPath = property.propertyPath;
            string errorMessage;

            // Use the reliable BeginChangeCheck/EndChangeCheck pattern to detect user modifications.
            EditorGUI.BeginChangeCheck();

            // The 'position' rect includes the total height (field + help box).
            // We need to calculate the rect for just the property field itself.
            Rect propertyRect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(property, label, true));
            EditorGUI.PropertyField(propertyRect, property, label, true);

            bool valueDidChange = EditorGUI.EndChangeCheck();

            // If the value was just changed by the user, we must re-validate it.
            if (valueDidChange)
            {
                ValidateProperty(property, out errorMessage);
                _validationErrorCache[propertyPath] = errorMessage;
            }
            // Otherwise, retrieve the current error state from the cache.
            else if (!_validationErrorCache.TryGetValue(propertyPath, out string cachedError))
            {
                // If not in cache, validate once to establish initial state.
                ValidateProperty(property, out errorMessage);
                _validationErrorCache[propertyPath] = errorMessage;
            }
            else
            {
                errorMessage = cachedError;
            }

            bool isValid = string.IsNullOrEmpty(errorMessage);

            // If the property is invalid, draw the visual feedback.
            if (!isValid)
            {
                // Draw a light red background to indicate an error.
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f, 1f); // Light red
                // Redraw the property field on top with the new background color.
                EditorGUI.PropertyField(propertyRect, property, label, true);
                GUI.backgroundColor = originalColor;

                // Draw the error message in a HelpBox directly below the field.
                float helpBoxHeight = GetHelpBoxHeight(errorMessage);
                Rect helpBoxPosition = new Rect(position.x, propertyRect.yMax + 2, position.width, helpBoxHeight);
                EditorGUI.HelpBox(helpBoxPosition, errorMessage, MessageType.Error);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Calculates the total height required for the property, including space for an error message if invalid.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            
            // To make the UI responsive, we must re-validate here to get the most current error state.
            // This ensures that when the user enters an invalid value, the inspector immediately reserves space for the help box on the next repaint.
            ValidateProperty(property, out string errorMessage);
            _validationErrorCache[property.propertyPath] = errorMessage;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Add space for the help box plus some padding.
                return baseHeight + GetHelpBoxHeight(errorMessage) + 4f; 
            }

            return baseHeight;
        }

        /// <summary>
        /// Validates the property using the decoupled architecture by asking its attributes to create validators.
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
                        // Use reflection to call the generic 'Validate' method.
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

        /// <summary>
        /// Calculates the height of a HelpBox based on its text content.
        /// </summary>
        private float GetHelpBoxHeight(string message)
        {
            var content = new GUIContent(message);
            var style = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
            // Calculate height based on the current inspector width, accounting for padding.
            float height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth - 40); 
            return Mathf.Max(EditorGUIUtility.singleLineHeight * 1.5f, height);
        }

        #region Reflection Helpers

        /// <summary>
        /// Gets the actual object value from a SerializedProperty using reflection. Handles nested properties and arrays/lists.
        /// </summary>
        private object GetValueFromProperty(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            foreach (var element in elements)
            {
                if (targetObject == null) return null;

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
        /// A generic reflection helper to get a value from an object by its field or property name.
        /// </summary>
        private object GetValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(source);

            var prop = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null) return prop.GetValue(source, null);
            
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
            
            // Simplify array paths (e.g., "myList.Array.data[0]" should return the FieldInfo for "myList").
            if (propertyPath.Contains(".Array.data["))
            {
                propertyPath = propertyPath.Substring(0, propertyPath.IndexOf(".Array.data["));
            }

            var pathParts = propertyPath.Split('.');
            FieldInfo fieldInfo = null;
            Type currentType = targetType;

            foreach (var part in pathParts)
            {
                if (currentType == null) return null;
                fieldInfo = currentType.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo == null) return null; // Path is invalid
                currentType = fieldInfo.FieldType; // Traverse down for the next part of the path
            }

            return fieldInfo;
        }
        #endregion
    }
}