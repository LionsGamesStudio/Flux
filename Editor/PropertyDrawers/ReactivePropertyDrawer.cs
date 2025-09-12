using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System.Reflection;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for ReactiveProperty attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(ReactivePropertyAttribute))]
    public class ReactivePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var reactiveAttribute = attribute as ReactivePropertyAttribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            // Add visual indicator for reactive properties
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 1f, 0.8f, 1f); // Light green background
            
            // Create label with reactive indicator
            var reactiveLabel = new GUIContent($"🔄 {label.text}", 
                $"Reactive Property Key: {reactiveAttribute.Key}\n{reactiveAttribute.Description}");
            
            EditorGUI.PropertyField(position, property, reactiveLabel);
            
            GUI.backgroundColor = originalColor;
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}