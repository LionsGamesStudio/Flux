using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomEditor(typeof(AttributedNodeWrapper))]
    public class AttributedNodeWrapperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var wrapper = target as AttributedNodeWrapper;
            if (wrapper == null || wrapper.NodeLogic == null) return;

            var logic = wrapper.NodeLogic;
            var logicType = logic.GetType();
            var nodeAttr = logicType.GetCustomAttribute<FluxNodeAttribute>();

            // --- Header ---
            EditorGUILayout.LabelField(nodeAttr?.DisplayName ?? logicType.Name, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(nodeAttr?.Description))
            {
                EditorGUILayout.HelpBox(nodeAttr.Description, MessageType.Info);
            }
            EditorGUILayout.Space();

            // Create a SerializedObject for the wrapper to handle Undo/Redo correctly
            var wrapperSO = new SerializedObject(wrapper);
            // Get the serialized property for our INode instance
            var logicProp = wrapperSO.FindProperty("_nodeLogic");

            // --- Configuration Fields ---
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            // Iterate through all serialized fields of the INode object
            if (logicProp.hasVisibleChildren)
            {
                var childProp = logicProp.Copy();
                var endProp = logicProp.GetEndProperty();
                childProp.NextVisible(true); // Enter the fist child

                while (!SerializedProperty.EqualContents(childProp, endProp))
                {
                    // Check if the field corresponding to this property has a [Port] attribute.
                    FieldInfo field = logicType.GetField(childProp.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.GetCustomAttribute<PortAttribute>() == null)
                    {
                        // If it's NOT a port, it's a configuration field. Draw it.
                        EditorGUILayout.PropertyField(childProp, true);
                    }
                    if (!childProp.NextVisible(false)) break;
                }
            }

            wrapperSO.ApplyModifiedProperties();
            
            EditorGUILayout.Space();

            var outputExecutionPorts = wrapper.OutputPorts.Where(p => p.PortType == FluxPortType.Execution).ToList();
            if (outputExecutionPorts.Any())
            {
                EditorGUILayout.LabelField("Execution Output Weights", EditorStyles.boldLabel);
                // We need to use a SerializedObject on the node itself to edit the port list.
                var nodeSO = new SerializedObject(wrapper);
                var outPortsProp = nodeSO.FindProperty("_outputPorts");

                for (int i = 0; i < outputExecutionPorts.Count; i++)
                {
                    var port = outputExecutionPorts[i];
                    var portProp = outPortsProp.GetArrayElementAtIndex(i); // This is risky if order changes.
                    if (portProp != null)
                    {
                        var weightProp = portProp.FindPropertyRelative("_probabilityWeight");
                        if (weightProp != null)
                        {
                            EditorGUILayout.PropertyField(weightProp, new GUIContent(port.DisplayName));
                        }
                    }
                }
                nodeSO.ApplyModifiedProperties();
            }
        }
    }
}