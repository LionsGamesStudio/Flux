using UnityEditor;
using UnityEngine;
using System;
using FluxFramework.Utils;

[CustomPropertyDrawer(typeof(StringIntPair))]
[CustomPropertyDrawer(typeof(StringFloatPair))]
[CustomPropertyDrawer(typeof(StringBoolPair))]
[CustomPropertyDrawer(typeof(StringStringPair))]
[CustomPropertyDrawer(typeof(StringVector2Pair))]
[CustomPropertyDrawer(typeof(StringVector3Pair))]
[CustomPropertyDrawer(typeof(StringGameObjectPair))]
[CustomPropertyDrawer(typeof(StringSpritePair))]
[CustomPropertyDrawer(typeof(StringMaterialPair))]
public class SerializableKeyValuePairDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // We draw the label "Element X" of the list
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // We save the indentation
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // We calculate the rectangles for the key and value
        var keyRect = new Rect(position.x, position.y, position.width * 0.45f, position.height);
        var valueRect = new Rect(position.x + position.width * 0.5f, position.y, position.width * 0.5f, position.height);

        // We draw the fields for the key and value without their own labels
        EditorGUI.PropertyField(keyRect, property.FindPropertyRelative("Key"), GUIContent.none);
        EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("Value"), GUIContent.none);

        // We restore the indentation
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}