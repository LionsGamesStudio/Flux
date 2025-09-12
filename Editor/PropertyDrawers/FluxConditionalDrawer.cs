using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;
using System.Reflection;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
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