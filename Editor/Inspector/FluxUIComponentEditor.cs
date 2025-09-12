#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FluxFramework.UI;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A custom inspector for FluxUIComponent and its children.
    /// It inherits all the functionality from the base FluxComponentEditor (groups, buttons)
    /// and adds a UI-specific "Binding Helper" tool.
    /// </summary>
    [CustomEditor(typeof(FluxUIComponent), true)]
    public class FluxUIComponentEditor : FluxComponentEditor
    {
        /// <summary>
        /// Overrides the base inspector GUI to add extra functionality.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 1. First, draw the entire base inspector (which handles groups, buttons, actions, etc.).
            base.OnInspectorGUI();

            // This section will only appear on components that inherit from FluxUIComponent.
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flux Binding Helper", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Show Available Property Keys"))
            {
                // This static method opens our custom editor window.
                PropertyKeyViewerWindow.ShowWindow();
            }
            
            EditorGUILayout.HelpBox("Use this tool to find and copy the correct property keys for your [FluxBinding] attributes to avoid typos.", MessageType.Info);
        }
    }
}
#endif