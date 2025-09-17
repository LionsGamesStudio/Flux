using UnityEditor;
using UnityEngine;
using FluxFramework.VisualScripting.Node;
using System.Linq;
using System.Collections.Generic;
using System;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomPropertyDrawer(typeof(CustomPortDefinition))]
    public class CustomPortDefinitionDrawer : PropertyDrawer
    {
        private static List<Type> _cachedTypes;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var portNameProp = property.FindPropertyRelative("PortName");
            var directionProp = property.FindPropertyRelative("Direction");
            var portTypeProp = property.FindPropertyRelative("PortType");
            var typeNameProp = property.FindPropertyRelative("ValueTypeName");

            // --- Draw the fields ---
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(rect, portNameProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            var halfWidth = position.width / 2f - 2;
            var halfRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
            EditorGUI.PropertyField(halfRect, directionProp);
            halfRect.x += halfWidth + 4;
            EditorGUI.PropertyField(halfRect, portTypeProp);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            // --- THE NEW PART: TYPE SELECTOR BUTTON ---
            if (portTypeProp.enumValueIndex == (int)FluxPortType.Data)
            {
                Type currentType = Type.GetType(typeNameProp.stringValue);
                string currentTypeName = (currentType != null) ? GetNiceTypeName(currentType) : "None (Click to select)";
                
                if (GUI.Button(rect, new GUIContent(currentTypeName, "Click to select a data type")))
                {
                    // Create a generic menu
                    var menu = new GenericMenu();
                    
                    // Cache all relevant types if not already done
                    if (_cachedTypes == null)
                    {
                        CacheAllTypes();
                    }

                    // Add types to the menu
                    foreach (var type in _cachedTypes)
                    {
                        menu.AddItem(new GUIContent(GetNiceMenuName(type)), type == currentType, () => {
                            typeNameProp.stringValue = type.AssemblyQualifiedName;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.ShowAsContext();
                }
            }
            
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var portTypeProp = property.FindPropertyRelative("PortType");
            int lineCount = 2; // Always show Name and Direction/Type
            if (portTypeProp.enumValueIndex == (int)FluxPortType.Data)
            {
                lineCount++; // Add a line for the Type field
            }
            return (EditorGUIUtility.singleLineHeight * lineCount) + (EditorGUIUtility.standardVerticalSpacing * (lineCount - 1));
        }

        private void CacheAllTypes()
        {
            _cachedTypes = new List<Type>();
            // Add common primitive types
            _cachedTypes.Add(typeof(float));
            _cachedTypes.Add(typeof(int));
            _cachedTypes.Add(typeof(bool));
            _cachedTypes.Add(typeof(string));
            // Add common Unity types
            _cachedTypes.Add(typeof(Vector2));
            _cachedTypes.Add(typeof(Vector3));
            _cachedTypes.Add(typeof(Color));
            _cachedTypes.Add(typeof(GameObject));
            _cachedTypes.Add(typeof(Transform));
            _cachedTypes.Add(typeof(UnityEngine.Object));
            
            // Add all INode types and other useful project types
            var projectTypes = TypeCache.GetTypesDerivedFrom<INode>().Concat(TypeCache.GetTypesDerivedFrom<MonoBehaviour>());
            _cachedTypes.AddRange(projectTypes.Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition).OrderBy(t => t.FullName));
            _cachedTypes = _cachedTypes.Distinct().ToList();
        }

        private string GetNiceTypeName(Type type)
        {
            if (type == null) return "None";
            // Add more friendly names as needed
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            return type.Name;
        }
        
        private string GetNiceMenuName(Type type)
        {
            // Create a path for the menu, e.g. "UnityEngine/GameObject"
            return type.FullName.Replace('.', '/');
        }
    }
}