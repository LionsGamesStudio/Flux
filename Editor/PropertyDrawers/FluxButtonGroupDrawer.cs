using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System.Reflection;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Property drawer for FluxButtonGroup attribute - displays FluxButton methods
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxButtonGroupAttribute))]
    public class FluxButtonGroupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var buttonGroupAttribute = attribute as FluxButtonGroupAttribute;
            
            // Check if we should show buttons based on play mode
            bool shouldShow = Application.isPlaying ? 
                buttonGroupAttribute.ShowInPlayMode : 
                buttonGroupAttribute.ShowInEditMode;
                
            if (!shouldShow)
            {
                // Still draw the original property
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            
            // Draw the original property first
            var propertyRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyRect, property, label);
            
            // Find all FluxButton methods in the target object
            var target = property.serializedObject.targetObject;
            var targetType = target.GetType();
            var fluxButtonMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<FluxButtonAttribute>() != null)
                .ToArray();
            
            if (fluxButtonMethods.Length > 0)
            {
                float yOffset = EditorGUIUtility.singleLineHeight + 5;
                
                // Draw header
                var headerRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(headerRect, $"🔧 {buttonGroupAttribute.Title}", EditorStyles.boldLabel);
                yOffset += EditorGUIUtility.singleLineHeight + 2;
                
                // Draw buttons
                foreach (var method in fluxButtonMethods)
                {
                    var buttonAttribute = method.GetCustomAttribute<FluxButtonAttribute>();
                    var buttonRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    
                    // Check if button should be enabled
                    bool buttonEnabled = Application.isPlaying ? 
                        buttonAttribute.EnabledInPlayMode : 
                        buttonAttribute.EnabledInEditMode;
                    
                    // Determine button text
                    string buttonText = string.IsNullOrEmpty(buttonAttribute.ButtonText) ? 
                        method.Name : 
                        buttonAttribute.ButtonText;
                    
                    GUI.enabled = buttonEnabled;
                    
                    if (GUI.Button(buttonRect, buttonText))
                    {
                        try
                        {
                            method.Invoke(target, null);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error invoking FluxButton method '{method.Name}': {e.Message}");
                        }
                    }
                    
                    GUI.enabled = true;
                    yOffset += EditorGUIUtility.singleLineHeight + 2;
                }
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var buttonGroupAttribute = attribute as FluxButtonGroupAttribute;
            
            // Check if we should show buttons based on play mode
            bool shouldShow = Application.isPlaying ? 
                buttonGroupAttribute.ShowInPlayMode : 
                buttonGroupAttribute.ShowInEditMode;
                
            if (!shouldShow)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            
            float height = EditorGUI.GetPropertyHeight(property, label);
            
            // Find all FluxButton methods to calculate additional height
            var target = property.serializedObject.targetObject;
            if (target != null)
            {
                var targetType = target.GetType();
                var fluxButtonMethods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttribute<FluxButtonAttribute>() != null)
                    .ToArray();
                
                if (fluxButtonMethods.Length > 0)
                {
                    // Add space for header + buttons
                    height += 5; // Initial spacing
                    height += EditorGUIUtility.singleLineHeight + 2; // Header
                    height += fluxButtonMethods.Length * (EditorGUIUtility.singleLineHeight + 2); // Buttons
                }
            }
            
            return height;
        }
    }
}