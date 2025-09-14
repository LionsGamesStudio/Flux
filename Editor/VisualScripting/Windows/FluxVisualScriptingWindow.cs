using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxVisualScriptingWindow : EditorWindow
    {
        private FluxGraphView _graphView;

        [MenuItem("Flux/Visual Scripting/Visual Scripting Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<FluxVisualScriptingWindow>();
            window.titleContent = new GUIContent("Flux Visual Scripting");
        }

        private void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Create and add the graph view
            _graphView = new FluxGraphView(this)
            {
                name = "Flux Graph View",
                style = { flexGrow = 1 } // Make it fill the window
            };
            root.Add(_graphView);
        }

        private void OnSelectionChange()
        {
            // When a FluxVisualGraph asset is selected in the Project window, load it.
            var graph = Selection.activeObject as FluxVisualGraph;
            if (graph != null && _graphView != null)
            {
                _graphView.PopulateView(graph);
            }
        }
    }
}