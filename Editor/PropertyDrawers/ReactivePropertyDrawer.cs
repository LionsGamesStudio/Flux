using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for the [ReactiveProperty] attribute.
    /// Enhanced to support dictionaries using the standard Unity approach with serializable key-value pairs.
    /// </summary>
    [CustomPropertyDrawer(typeof(ReactivePropertyAttribute))]
    public class ReactivePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var reactiveAttribute = attribute as ReactivePropertyAttribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            // --- Style and Label Setup ---
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f, 1f); // Light green background
            
            var reactiveLabel = new GUIContent($"🔄 {label.text}", 
                $"Reactive Property Key: {reactiveAttribute.Key}\n{reactiveAttribute.Description}");
            
            // Search for the internal property that contains the actual value
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");

            if (valueProperty != null)
            {
                // Check if this is a dictionary type
                if (IsDictionaryType(valueProperty))
                {
                    DrawDictionaryProperty(position, valueProperty, reactiveLabel);
                }
                else
                {
                    // Standard handling for lists and other types
                    EditorGUI.PropertyField(position, valueProperty, reactiveLabel, true);
                }
            }
            else
            {
                // Fallback: If "_value" is not found, revert to default behavior
                EditorGUI.PropertyField(position, property, reactiveLabel, true);
            }
            
            GUI.backgroundColor = originalColor;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProperty = property.FindPropertyRelative("_value");

            if (valueProperty != null)
            {
                if (IsDictionaryType(valueProperty))
                {
                    return GetDictionaryPropertyHeight(valueProperty, label);
                }
                else
                {
                    return EditorGUI.GetPropertyHeight(valueProperty, label, true);
                }
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        private bool IsDictionaryType(SerializedProperty property)
        {
            // Check if the property type name contains "Dictionary" or "SerializableDictionary"
            return property.type.Contains("Dictionary") || property.type.Contains("SerializableDictionary");
        }

        private void DrawDictionaryProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            // Look for SerializableDictionary structure (keys and values lists)
            SerializedProperty keysProperty = property.FindPropertyRelative("keys");
            SerializedProperty valuesProperty = property.FindPropertyRelative("values");
            
            if (keysProperty != null && valuesProperty != null)
            {
                // Draw as expandable list with proper key-value pairs
                DrawSerializableDictionaryAsReorderableList(position, property, keysProperty, valuesProperty, label);
            }
            else
            {
                // Fallback to default property drawer
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private void DrawSerializableDictionaryAsReorderableList(Rect position, SerializedProperty property, 
            SerializedProperty keysProperty, SerializedProperty valuesProperty, GUIContent label)
        {
            // Simple foldout approach
            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            int entryCount = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
            string displayLabel = $"{label.text} (Dictionary: {entryCount} entries)";
            
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, displayLabel, true);
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float currentY = foldoutRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                // Add/Remove buttons in a clean row
                Rect buttonsRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                DrawDictionaryButtons(buttonsRect, keysProperty, valuesProperty);
                currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                // Draw each key-value pair
                for (int i = 0; i < entryCount; i++)
                {
                    Rect entryRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                    DrawDictionaryEntry(entryRect, keysProperty, valuesProperty, i);
                    currentY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawDictionaryButtons(Rect rect, SerializedProperty keysProperty, SerializedProperty valuesProperty)
        {
            float buttonWidth = 80f;
            float spacing = 5f;
            
            Rect addRect = new Rect(rect.x, rect.y, buttonWidth, rect.height);
            Rect removeRect = new Rect(rect.x + buttonWidth + spacing, rect.y, buttonWidth, rect.height);
            
            // Add button
            if (GUI.Button(addRect, "Add Entry"))
            {
                int newIndex = keysProperty.arraySize;
                keysProperty.InsertArrayElementAtIndex(newIndex);
                valuesProperty.InsertArrayElementAtIndex(newIndex);
                
                // Initialize new elements to default values
                SerializedProperty newKey = keysProperty.GetArrayElementAtIndex(newIndex);
                SerializedProperty newValue = valuesProperty.GetArrayElementAtIndex(newIndex);
                
                // Special handling for enum keys to allow free selection
                SetPropertyToDefaultWithEnumSupport(newKey);
                SetPropertyToDefault(newValue);
                
                // Apply changes immediately
                keysProperty.serializedObject.ApplyModifiedProperties();
            }
            
            // Remove button (only if there are entries)
            EditorGUI.BeginDisabledGroup(keysProperty.arraySize == 0);
            if (GUI.Button(removeRect, "Remove Last"))
            {
                if (keysProperty.arraySize > 0 && valuesProperty.arraySize > 0)
                {
                    int lastIndex = keysProperty.arraySize - 1;
                    keysProperty.DeleteArrayElementAtIndex(lastIndex);
                    
                    // Ensure values array is in sync
                    if (valuesProperty.arraySize > keysProperty.arraySize)
                    {
                        valuesProperty.DeleteArrayElementAtIndex(valuesProperty.arraySize - 1);
                    }
                    
                    // Apply changes immediately
                    keysProperty.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawDictionaryEntry(Rect rect, SerializedProperty keysProperty, SerializedProperty valuesProperty, int index)
        {
            // Validate index bounds
            if (index >= keysProperty.arraySize || index >= valuesProperty.arraySize)
                return;
                
            SerializedProperty keyElement = keysProperty.GetArrayElementAtIndex(index);
            SerializedProperty valueElement = valuesProperty.GetArrayElementAtIndex(index);
            
            float keyWidth = rect.width * 0.4f;
            float valueWidth = rect.width * 0.4f;
            float spacing = 10f;
            float removeButtonWidth = 20f;
            
            // Key field - special handling for enums to show all options
            Rect keyRect = new Rect(rect.x, rect.y, keyWidth, rect.height);
            if (keyElement.propertyType == SerializedPropertyType.Enum)
            {
                // For enums, show dropdown with all available values
                DrawEnumKeyField(keyRect, keyElement, keysProperty, index);
            }
            else
            {
                EditorGUI.PropertyField(keyRect, keyElement, GUIContent.none);
            }
            
            // Arrow separator
            Rect arrowRect = new Rect(rect.x + keyWidth + 5f, rect.y, 20f, rect.height);
            EditorGUI.LabelField(arrowRect, "→", EditorStyles.centeredGreyMiniLabel);
            
            // Value field
            Rect valueRect = new Rect(rect.x + keyWidth + 25f, rect.y, valueWidth, rect.height);
            EditorGUI.PropertyField(valueRect, valueElement, GUIContent.none);
            
            // Remove button for this specific entry
            Rect removeRect = new Rect(rect.x + rect.width - removeButtonWidth, rect.y, removeButtonWidth, rect.height);
            if (GUI.Button(removeRect, "×"))
            {
                keysProperty.DeleteArrayElementAtIndex(index);
                if (index < valuesProperty.arraySize)
                {
                    valuesProperty.DeleteArrayElementAtIndex(index);
                }
                keysProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawEnumKeyField(Rect rect, SerializedProperty keyElement, SerializedProperty keysProperty, int currentIndex)
        {
            // Get current enum value
            int currentEnumValue = keyElement.enumValueIndex;
            string[] enumNames = keyElement.enumNames;
            
            // Check for duplicates and highlight them
            bool hasDuplicate = HasDuplicateEnumKey(keysProperty, currentIndex, currentEnumValue);
            
            if (hasDuplicate)
            {
                // Highlight in red if duplicate
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                currentEnumValue = EditorGUI.Popup(rect, currentEnumValue, enumNames);
                GUI.backgroundColor = originalColor;
            }
            else
            {
                currentEnumValue = EditorGUI.Popup(rect, currentEnumValue, enumNames);
            }
            
            // Apply the change
            keyElement.enumValueIndex = currentEnumValue;
        }

        private bool HasDuplicateEnumKey(SerializedProperty keysProperty, int currentIndex, int enumValue)
        {
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                if (i != currentIndex)
                {
                    SerializedProperty otherKey = keysProperty.GetArrayElementAtIndex(i);
                    if (otherKey.propertyType == SerializedPropertyType.Enum && 
                        otherKey.enumValueIndex == enumValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void SetPropertyToDefaultWithEnumSupport(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Enum:
                    // For enums, find the first value that's not already used
                    SetToUnusedEnumValue(property);
                    break;
                default:
                    SetPropertyToDefault(property);
                    break;
            }
        }

        private void SetToUnusedEnumValue(SerializedProperty enumProperty)
        {
            // Get the parent keys array to check for existing values
            SerializedProperty keysProperty = GetParentKeysProperty(enumProperty);
            
            if (keysProperty != null)
            {
                // Find the first enum value that's not already used
                string[] enumNames = enumProperty.enumNames;
                for (int enumIndex = 0; enumIndex < enumNames.Length; enumIndex++)
                {
                    bool isUsed = false;
                    for (int i = 0; i < keysProperty.arraySize - 1; i++) // -1 because we just added a new one
                    {
                        SerializedProperty existingKey = keysProperty.GetArrayElementAtIndex(i);
                        if (existingKey.propertyType == SerializedPropertyType.Enum && 
                            existingKey.enumValueIndex == enumIndex)
                        {
                            isUsed = true;
                            break;
                        }
                    }
                    
                    if (!isUsed)
                    {
                        enumProperty.enumValueIndex = enumIndex;
                        return;
                    }
                }
            }
            
            // If all values are used or we can't determine, set to first
            enumProperty.enumValueIndex = 0;
        }

        private SerializedProperty GetParentKeysProperty(SerializedProperty enumProperty)
        {
            // Navigate up to find the keys array
            string path = enumProperty.propertyPath;
            if (path.Contains(".Array.data["))
            {
                string keysPath = path.Substring(0, path.LastIndexOf(".Array.data["));
                return enumProperty.serializedObject.FindProperty(keysPath);
            }
            return null;
        }

        private void SetPropertyToDefault(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    property.stringValue = "";
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                default:
                    // For other types, try to reset if possible
                    break;
            }
        }

        private float GetDictionaryPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // Foldout line
            
            if (property.isExpanded)
            {
                SerializedProperty keysProperty = property.FindPropertyRelative("keys");
                SerializedProperty valuesProperty = property.FindPropertyRelative("values");
                
                if (keysProperty != null && valuesProperty != null)
                {
                    // Height for buttons line
                    height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    
                    // Height for each entry
                    int entryCount = Mathf.Min(keysProperty.arraySize, valuesProperty.arraySize);
                    height += entryCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                }
                else
                {
                    // Fallback height
                    height += EditorGUIUtility.singleLineHeight * 2;
                }
            }
            
            return height;
        }
    }
}