using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System.Reflection;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for FluxReadOnly attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxReadOnlyAttribute))]
    public class FluxReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var readOnlyAttribute = attribute as FluxReadOnlyAttribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            if (readOnlyAttribute.GrayOut)
            {
                // Show field grayed out
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label);
                GUI.enabled = true;
            }
            else
            {
                // Show as label only
                var valueText = GetPropertyValueAsString(property);
                EditorGUI.LabelField(position, label.text, valueText);
            }
            
            // Show message if provided
            if (!string.IsNullOrEmpty(readOnlyAttribute.Message))
            {
                var messagePosition = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, 
                    position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(messagePosition, "🔒 " + readOnlyAttribute.Message, EditorStyles.miniLabel);
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var readOnlyAttribute = attribute as FluxReadOnlyAttribute;
            float baseHeight = EditorGUI.GetPropertyHeight(property, label);
            
            if (!string.IsNullOrEmpty(readOnlyAttribute.Message))
            {
                return baseHeight + EditorGUIUtility.singleLineHeight + 2;
            }
            
            return baseHeight;
        }
        
        private string GetPropertyValueAsString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("F2");
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                default:
                    return property.displayName;
            }
        }
    }
}