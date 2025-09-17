using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxVisualScriptingWindow : EditorWindow
    {
        private FluxInspectorView _inspectorView;
        private Label _graphNameLabel;

        public FluxGraphView GraphView { get; private set; } 

        [MenuItem("Flux/Visual Scripting/Visual Scripting Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<FluxVisualScriptingWindow>();
            window.titleContent = new GUIContent("Flux Visual Scripting");
        }
        
        public static void OpenWithGraph(FluxVisualGraph graph)
        {
            var window = GetWindow<FluxVisualScriptingWindow>();
            window.titleContent = new GUIContent("Flux Visual Scripting");
            
            Selection.activeObject = graph;
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            // --- 1. Create the Toolbar ---
            var toolbar = new Toolbar();
            root.Add(toolbar);

            // --- 2. Create Toolbar Menus ---
            var fileMenu = new ToolbarMenu { text = "File" };
            fileMenu.menu.AppendAction("New Graph...", (a) => CreateNewGraph());
            fileMenu.menu.AppendAction("Open Graph...", (a) => OpenGraphAsset());
            toolbar.Add(fileMenu);

            // --- 3. Add a label for the current graph ---
            _graphNameLabel = new Label("No Graph Loaded");
            _graphNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _graphNameLabel.style.marginLeft = 10;
            toolbar.Add(_graphNameLabel);

            // --- 4. The rest of the layout ---
            var splitView = new TwoPaneSplitView(0, 1000, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            GraphView = new FluxGraphView(this);
            splitView.Add(GraphView);

            _inspectorView = new FluxInspectorView();
            splitView.Add(_inspectorView);

            OnSelectionChange();
        }

        public void OnElementSelected(GraphElement element)
        {
            _inspectorView?.UpdateSelection(element);
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Flux Graph", 
                "NewFluxGraph.asset", 
                "asset", 
                "Please enter a file name to save the new graph to.");

            if (string.IsNullOrEmpty(path)) return;

            var newGraph = ScriptableObject.CreateInstance<FluxVisualGraph>();
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            
            // Automatically select and load the new graph
            Selection.activeObject = newGraph;
        }

        private void OpenGraphAsset()
        {
            string path = EditorUtility.OpenFilePanel("Open Flux Graph", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute path to a project-relative path.
            if (path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                var graph = AssetDatabase.LoadAssetAtPath<FluxVisualGraph>(relativePath);
                if (graph != null)
                {
                    Selection.activeObject = graph; // Selecting it will trigger OnSelectionChange and load it.
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Error", "The selected asset is not a valid Flux Visual Graph.", "OK");
                }
            }
        }

        private void OnSelectionChange()
        {
            // When a FluxVisualGraph asset is selected in the Project window, load it.
            var graph = Selection.activeObject as FluxVisualGraph;
            if (graph != null && GraphView != null)
            {
                GraphView.PopulateView(graph);
                _graphNameLabel.text = $"Editing: {graph.name}";
            }
            else
            {
                // Clear the graph view if no valid graph is selected
                GraphView?.PopulateView(null);
                _graphNameLabel.text = "No Graph Loaded";
            }
        }
    }
}