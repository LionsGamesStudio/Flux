using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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

            // Use a SerializedObject to get proper Undo/Redo and prefab support.
            var wrapperSO = new SerializedObject(wrapper);
            var logicProp = wrapperSO.FindProperty("_nodeLogic");

            // --- Configuration Fields ---
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            // Track if any changes were made to trigger a port rebuild.
            EditorGUI.BeginChangeCheck();

            // Iterate through all serialized fields of the INode object
            if (logicProp.hasVisibleChildren)
            {
                var childProp = logicProp.Copy();
                var endProp = logicProp.GetEndProperty();
                childProp.NextVisible(true);

                while (!SerializedProperty.EqualContents(childProp, endProp))
                {
                    FieldInfo field = logicType.GetField(childProp.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.GetCustomAttribute<PortAttribute>() == null)
                    {
                        EditorGUILayout.PropertyField(childProp, true);
                    }
                    if (!childProp.NextVisible(false)) break;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                wrapperSO.ApplyModifiedProperties();
            }

            // --- DYNAMIC PORT MANAGEMENT ---
            if (logic is IPortConfiguration)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This node has dynamic ports. Click 'Update Ports' after modifying its configuration.", MessageType.Info);

                if (GUILayout.Button("Update Ports"))
                {
                    // 1. Update the data model in memory.
                    wrapper.RebuildPorts();
                    
                    // 2. Force Unity to save the changes made to this ScriptableObject asset to disk.
                    // This is the most important step.
                    EditorUtility.SetDirty(wrapper);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    // 3. Now that the data on disk is correct, tell the graph view to reload.
                    // When it reloads, it will read the FRESH version of the asset from disk,
                    // which includes the new ports.
                    var window = EditorWindow.GetWindow<FluxVisualScriptingWindow>();
                    window.GraphView?.Refresh();
                }
            }
            
            EditorGUILayout.Space();

            // --- EXECUTION OUTPUT WEIGHTS ---
            var nodeSO = new SerializedObject(wrapper);
            var outPortsProp = nodeSO.FindProperty("_outputPorts");
            
            // Find all execution output ports to display their weights.
            var outputExecutionPorts = new List<(SerializedProperty prop, string name)>();
            for(int i = 0; i < outPortsProp.arraySize; i++)
            {
                var portProp = outPortsProp.GetArrayElementAtIndex(i);
                var portTypeProp = portProp.FindPropertyRelative("_portType");
                if (portTypeProp.enumValueIndex == (int)FluxPortType.Execution)
                {
                    outputExecutionPorts.Add((portProp, portProp.FindPropertyRelative("_displayName").stringValue));
                }
            }

            if (outputExecutionPorts.Count > 1) // Only show weights if there's a choice to be made.
            {
                EditorGUILayout.LabelField("Execution Output Weights", EditorStyles.boldLabel);
                
                foreach (var (prop, name) in outputExecutionPorts)
                {
                    var weightProp = prop.FindPropertyRelative("_probabilityWeight");
                    if (weightProp != null)
                    {
                        EditorGUILayout.PropertyField(weightProp, new GUIContent(name));
                    }
                }
                
                nodeSO.ApplyModifiedProperties();
            }
        }
    }
}