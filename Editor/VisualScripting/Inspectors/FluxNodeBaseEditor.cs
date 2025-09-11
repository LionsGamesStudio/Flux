using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Editor.Inspectors
{
    /// <summary>
    /// Base custom editor for FluxNodeBase with common functionality
    /// </summary>
    [CustomEditor(typeof(FluxNodeBase), true)]
    public class FluxNodeBaseEditor : UnityEditor.Editor
    {
        protected SerializedProperty _customDisplayNameProp;
        protected SerializedProperty _nodeNameProp;
        protected SerializedProperty _categoryProp;

        protected virtual void OnEnable()
        {
            _customDisplayNameProp = serializedObject.FindProperty("_customDisplayName");
            _nodeNameProp = serializedObject.FindProperty("_nodeName");
            _categoryProp = serializedObject.FindProperty("_category");
        }

        public override void OnInspectorGUI()
        {
            // Check if this is a specialized node type that has its own editor
            var targetType = target.GetType();
            if (HasSpecializedEditor(targetType))
            {
                // Let the specialized editor handle it
                return;
            }

            // Default inspector for FluxNodeBase
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Node: {targetType.Name}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Custom display name
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_customDisplayNameProp, new GUIContent("Custom Name"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            // Show node info
            EditorGUILayout.LabelField("Node Info", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            var node = target as FluxNodeBase;
            EditorGUILayout.TextField("Node ID", node.NodeId);
            EditorGUILayout.TextField("Default Name", node.DefaultNodeName);
            EditorGUILayout.TextField("Category", node.Category);
            EditorGUILayout.Vector2Field("Position", node.Position);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Show ports info
            ShowPortsInfo(node);

            EditorGUILayout.Space();

            // Draw default inspector for other properties
            DrawPropertiesExcluding(serializedObject, 
                "_customDisplayName", "_nodeName", "_category", "_nodeId", "_position", "_inputPorts", "_outputPorts");

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void ShowPortsInfo(FluxNodeBase node)
        {
            EditorGUILayout.LabelField("Ports", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField($"Input Ports: {node.InputPorts.Count}");
            foreach (var port in node.InputPorts)
            {
                EditorGUILayout.TextField($"  {port.DisplayName}", $"{port.PortType} ({port.ValueType})");
            }
            
            EditorGUILayout.LabelField($"Output Ports: {node.OutputPorts.Count}");
            foreach (var port in node.OutputPorts)
            {
                EditorGUILayout.TextField($"  {port.DisplayName}", $"{port.PortType} ({port.ValueType})");
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool HasSpecializedEditor(System.Type nodeType)
        {
            // Check if there's a more specific CustomEditor for this type
            return nodeType == typeof(FluxFramework.VisualScripting.Nodes.ConstantNode) ||
                   nodeType == typeof(FluxFramework.VisualScripting.Nodes.EventListenerNode) ||
                   nodeType == typeof(FluxFramework.VisualScripting.Nodes.EventPublishNode);
        }
    }
}
