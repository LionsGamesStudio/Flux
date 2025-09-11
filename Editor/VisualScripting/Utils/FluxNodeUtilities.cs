using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting;

namespace FluxFramework.Editor.VisualScripting.Utils
{
    /// <summary>
    /// Provides editor menu items for utility functions related to visual scripting nodes,
    /// such as bulk refreshing of node ports.
    /// </summary>
    public static class FluxNodeUtilities
    {
        private const string MENU_ROOT = "Assets/Flux/Visual Scripting/";

        /// <summary>
        /// Validates if the "Refresh Ports" menu item should be enabled.
        /// </summary>
        [MenuItem(MENU_ROOT + "Refresh Selected Node Ports", true)]
        private static bool RefreshSelectedNodePortsValidation()
        {
            return Selection.activeObject is FluxNodeBase;
        }

        /// <summary>
        /// A safe refresh that attempts to preserve connections.
        /// </summary>
        [MenuItem(MENU_ROOT + "Refresh Selected Node Ports")]
        private static void RefreshSelectedNodePorts()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is FluxNodeBase node)
                {
                    // Calling OnValidate manually is a good way to trigger the non-destructive
                    // port validation logic that runs OnEnable. We need reflection for this.
                    var onValidateMethod = node.GetType().GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    onValidateMethod?.Invoke(node, null);

                    EditorUtility.SetDirty(node);
                    Debug.Log($"Refreshed ports for {node.NodeName}", node);
                }
            }
        }

        /// <summary>
        /// Validates if the "Force Regenerate Ports" menu item should be enabled.
        /// </summary>
        [MenuItem(MENU_ROOT + "Force Regenerate Selected Node Ports", true)]
        private static bool ForceRegenerateSelectedNodePortsValidation()
        {
            return Selection.activeObject is FluxNodeBase;
        }

        /// <summary>
        /// A destructive refresh that rebuilds ports from scratch.
        /// </summary>
        [MenuItem(MENU_ROOT + "Force Regenerate Selected Node Ports")]
        private static void ForceRegenerateSelectedNodePorts()
        {
             if (!EditorUtility.DisplayDialog(
                "Force Regenerate Ports?",
                "This will completely rebuild the ports for the selected nodes. This may break existing connections if port names have changed.\n\nAre you sure you want to continue?",
                "Yes, Regenerate",
                "Cancel"))
            {
                return;
            }

            foreach (var obj in Selection.objects)
            {
                if (obj is FluxNodeBase node)
                {
                    // This is the call to the method we just added.
                    node.ForceRegeneratePorts();
                    EditorUtility.SetDirty(node);
                }
            }
        }
    }
}