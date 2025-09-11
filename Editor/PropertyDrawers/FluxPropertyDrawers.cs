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

    /// <summary>
    /// Property drawer for FluxConditional attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(FluxConditionalAttribute))]
    public class FluxConditionalDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var conditionalAttribute = attribute as FluxConditionalAttribute;
            
            EditorGUI.BeginProperty(position, label, property);
            
            // Check condition
            bool shouldShow = EvaluateCondition(property, conditionalAttribute);
            
            if (shouldShow)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var conditionalAttribute = attribute as FluxConditionalAttribute;
            bool shouldShow = EvaluateCondition(property, conditionalAttribute);
            
            return shouldShow ? EditorGUI.GetPropertyHeight(property, label) : 0;
        }
        
        private bool EvaluateCondition(SerializedProperty property, FluxConditionalAttribute attribute)
        {
            var conditionProperty = property.serializedObject.FindProperty(attribute.ConditionField);
            
            if (conditionProperty == null)
            {
                return true; // Show by default if condition field not found
            }
            
            bool conditionMet = false;
            
            // Evaluate based on property type
            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    conditionMet = conditionProperty.boolValue == (bool)attribute.ExpectedValue;
                    break;
                case SerializedPropertyType.Integer:
                    conditionMet = conditionProperty.intValue == Convert.ToInt32(attribute.ExpectedValue);
                    break;
                case SerializedPropertyType.String:
                    conditionMet = conditionProperty.stringValue == attribute.ExpectedValue.ToString();
                    break;
                case SerializedPropertyType.Enum:
                    conditionMet = conditionProperty.enumValueIndex == Convert.ToInt32(attribute.ExpectedValue);
                    break;
                default:
                    conditionMet = true;
                    break;
            }
            
            return attribute.ShowWhenTrue ? conditionMet : !conditionMet;
        }
    }
}
