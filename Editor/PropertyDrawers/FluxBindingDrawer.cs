using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System.Reflection;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for FluxBinding attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxBindingAttribute))]
    public class FluxBindingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var bindingAttribute = attribute as FluxBindingAttribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            // Add visual indicator for binding
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.8f, 1f, 1f); // Light blue background
            
            // Create label with binding indicator
            var bindingLabel = new GUIContent($"🔗 {label.text}", 
                $"Binding Key: {bindingAttribute.PropertyKey}\nMode: {bindingAttribute.Mode}");
            
            EditorGUI.PropertyField(position, property, bindingLabel);
            
            GUI.backgroundColor = originalColor;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}