using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor.Inspectors
{
    /// <summary>
    /// A clean, informative custom editor for the StartNode.
    /// </summary>
    [CustomEditor(typeof(StartNode))]
    public class StartNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // We use SerializedObject for robust property handling.
            serializedObject.Update();

            EditorGUILayout.Space();
            
            // An info box explaining the purpose of the StartNode.
            EditorGUILayout.HelpBox(
                "This node is a primary entry point for a graph's execution. " +
                "The graph will automatically start running from this node's 'Start' output port.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // Display the default properties from the base node editor.
            // This is cleaner than drawing them manually and will automatically
            // include things like 'Custom Name' if you add it to the base editor.
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}