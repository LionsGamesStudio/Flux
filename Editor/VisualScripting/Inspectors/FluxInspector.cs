using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxInspectorView : VisualElement
    {
        private UnityEditor.Editor _editor;

        public FluxInspectorView()
        {
            // Create a container for the inspector
            var container = new IMGUIContainer(() =>
            {
                if (_editor != null && _editor.target != null)
                {
                    _editor.OnInspectorGUI();
                }
            });
            Add(container);
        }

        /// <summary>
        /// Updates the view to inspect a new node.
        /// </summary>
        /// <summary>
        /// Updates the view to inspect a graph element (Node or Edge).
        /// </summary>
        public void UpdateSelection(GraphElement element)
        {
            // Clean up the old editor
            if (_editor != null)
            {
                UnityEngine.Object.DestroyImmediate(_editor);
                _editor = null;
            }

            if (element is FluxNodeView nodeView)
            {
                _editor = UnityEditor.Editor.CreateEditor(nodeView.Node);
            }
            else if (element is Edge edge)
            {
                // Find the FluxNodeConnection that this Edge represents
                var graphView = edge.GetFirstAncestorOfType<FluxGraphView>();
                var connectionData = graphView.GetConnectionDataForEdge(edge);
                if (connectionData != null)
                {
                    // Create our temporary proxy object to be the target of the editor.
                    var proxy = ScriptableObject.CreateInstance<ConnectionProxy>();
                    proxy.Initialize(connectionData);
                    _editor = UnityEditor.Editor.CreateEditor(proxy);
                }
            }
            else
            {
                // If nothing (or something else) is selected, ensure the inspector is cleared.
                // This might have been the missing piece.
                if (_editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(_editor);
                    _editor = null;
                }
            }
        }
    }
}