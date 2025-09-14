using UnityEngine.UIElements;
using UnityEditor;
using System.Reflection;
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
        public void UpdateSelection(FluxNodeView nodeView)
        {
            // Clean up the old editor
            if (_editor != null)
            {
                UnityEngine.Object.DestroyImmediate(_editor);
                _editor = null;
            }

            if (nodeView == null) return;
            
            // Create a new editor for the selected node's data model
            _editor = UnityEditor.Editor.CreateEditor(nodeView.Node);
        }
    }
}