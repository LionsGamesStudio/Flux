using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// The main editor window for the Flux Visual Scripting system.
    /// It hosts the graph view for editing, the node inspector for configuration,
    /// and the main toolbar for graph management actions.
    /// </summary>
    public class FluxVisualScriptingWindow : EditorWindow
    {
        private FluxGraphView _graphView;
        private FluxVisualGraph _currentGraph;
        private FluxInspectorView _inspectorView;

        /// <summary>
        /// The currently loaded graph asset in the editor. Setting this property
        /// will automatically load and display the graph.
        /// </summary>
        public FluxVisualGraph CurrentGraph 
        { 
            get => _currentGraph; 
            set => LoadGraph(value); 
        }

        /// <summary>
        /// Opens the Visual Scripting Editor window from the Unity menu.
        /// </summary>
        [MenuItem("Flux/Visual Scripting Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<FluxVisualScriptingWindow>("Flux Visual Scripting");
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// A static method to open the editor window and load a specific graph.
        /// This is useful for entry points like the "Open in Editor" button on the graph asset.
        /// </summary>
        public static void OpenWithGraph(FluxVisualGraph graph)
        {
            var window = GetWindow<FluxVisualScriptingWindow>("Flux Visual Scripting");
            window.minSize = new Vector2(800, 600);
            window.Focus();
            
            // Loading is delayed to ensure the window's GUI has been fully created.
            EditorApplication.delayCall += () =>
            {
                if (window != null)
                {
                    window.LoadGraph(graph);
                }
            };
        }

        /// <summary>
        /// Called by Unity to create the editor window's UI using UI Toolkit.
        /// </summary>
        private void CreateGUI()
        {
            var root = rootVisualElement;
            
            var toolbar = new Toolbar();
            CreateToolbar(toolbar);
            root.Add(toolbar);

            var contentContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, flexGrow = 1 }
            };

            _graphView = new FluxGraphView(this)
            {
                name = "Flux Graph View",
                style = { flexGrow = 1 }
            };
            contentContainer.Add(_graphView);

            _inspectorView = new FluxInspectorView
            {
                name = "Flux Inspector View",
                style = { width = 300 }
            };
            contentContainer.Add(_inspectorView);

            root.Add(contentContainer);

            LoadLastGraph();
        }

        /// <summary>
        /// Populates the main toolbar with menus and buttons.
        /// </summary>
        private void CreateToolbar(Toolbar toolbar)
        {
            var fileMenu = new ToolbarMenu { text = "File" };
            fileMenu.menu.AppendAction("New Graph...", _ => CreateNewGraph());
            fileMenu.menu.AppendAction("Open Graph...", _ => OpenGraph());
            fileMenu.menu.AppendSeparator();
            fileMenu.menu.AppendAction("Save Graph", _ => SaveGraph(), CanPerformGraphAction);
            fileMenu.menu.AppendAction("Save Graph As...", _ => SaveGraphAs(), CanPerformGraphAction);
            toolbar.Add(fileMenu);

            var editMenu = new ToolbarMenu { text = "Edit" };
            editMenu.menu.AppendAction("Validate Graph", _ => ValidateGraph(), CanPerformGraphAction);
            editMenu.menu.AppendAction("Execute Graph (Test)", _ => ExecuteGraph(), CanExecuteGraph);
            toolbar.Add(editMenu);
            
            var viewMenu = new ToolbarMenu { text = "View" };
            viewMenu.menu.AppendAction("Frame All", _ => _graphView?.FrameAll(), CanPerformGraphAction);
            viewMenu.menu.AppendAction("Frame Selection", _ => _graphView?.FrameSelection(), CanPerformGraphAction);
            toolbar.Add(viewMenu);

            var graphNameLabel = new Label("No Graph Loaded")
            {
                name = "graph-name-label",
                style = { marginLeft = 10, unityTextAlign = TextAnchor.MiddleLeft }
            };
            toolbar.Add(graphNameLabel);
            
            toolbar.Add(new ToolbarSpacer { flex = true });

            var helpButton = new Button(() => Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/VISUAL_SCRIPTING_README.md"))
            {
                text = "Help"
            };
            toolbar.Add(helpButton);
        }

        private DropdownMenuAction.Status CanPerformGraphAction(DropdownMenuAction action) =>
            _currentGraph != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

        private DropdownMenuAction.Status CanExecuteGraph(DropdownMenuAction action) =>
            _currentGraph != null && Application.isPlaying ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Flux Graph", "NewFluxGraph", "asset", "Please enter a file name to save the graph to.");
            if (string.IsNullOrEmpty(path)) return;
            
            var graph = CreateInstance<FluxVisualGraph>();
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            LoadGraph(graph);
        }

        private void OpenGraph()
        {
            string path = EditorUtility.OpenFilePanel("Open Flux Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute path to a project-relative path.
            if (path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                var graph = AssetDatabase.LoadAssetAtPath<FluxVisualGraph>(relativePath);
                if (graph != null)
                {
                    LoadGraph(graph);
                }
                else
                {
                    EditorUtility.DisplayDialog("Load Error", "The selected asset is not a valid Flux Visual Graph.", "OK");
                }
            }
        }

        private void SaveGraph()
        {
            if (_currentGraph != null)
            {
                // The graph view's changes have already updated the ScriptableObject in memory.
                // We just need to mark it as dirty and save the project's assets to disk.
                EditorUtility.SetDirty(_currentGraph);
                AssetDatabase.SaveAssets();
                ShowNotification(new GUIContent("Graph Saved!"), 1.0);
            }
        }

        private void SaveGraphAs()
        {
            if (_currentGraph == null) return;
            string path = EditorUtility.SaveFilePanelInProject("Save Graph As...", _currentGraph.name, "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            var newGraph = Instantiate(_currentGraph);
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            LoadGraph(newGraph);
        }

        private void ValidateGraph()
        {
            if (!CanPerformGraphAction(null).HasFlag(DropdownMenuAction.Status.Normal)) return;
            bool isValid = _currentGraph.Validate();
            string message = isValid ? "Graph validation passed successfully!" : "Graph validation failed. Check the console for details on unconnected required ports.";
            EditorUtility.DisplayDialog("Graph Validation", message, "OK");
        }

        /// <summary>
        /// Test-executes the current graph by finding or creating a temporary runner in the scene.
        /// This is for debugging purposes only and requires Play Mode.
        /// </summary>
        private void ExecuteGraph()
        {
            if (!CanExecuteGraph(null).HasFlag(DropdownMenuAction.Status.Normal)) return;

            var existingRunner = FindFirstObjectByType<FluxVisualScriptComponent>();
            if (existingRunner != null && existingRunner.Graph == _currentGraph)
            {
                Debug.Log($"[FluxVS] Executing graph via existing component '{existingRunner.gameObject.name}'.", existingRunner.gameObject);
                existingRunner.ExecuteGraph();
            }
            else
            {
                Debug.LogWarning($"[FluxVS] No specific runner found for '{_currentGraph.name}'. Creating a temporary runner for test execution.", _currentGraph);
                var tempRunnerGO = new GameObject($"__TempGraphRunner_{_currentGraph.name}");
                var tempRunnerComp = tempRunnerGO.AddComponent<FluxVisualScriptComponent>();
                tempRunnerComp.Graph = _currentGraph;
                tempRunnerComp.ExecuteGraph();
            }
            ShowNotification(new GUIContent("Graph Execution Triggered!"), 1.0);
        }

        public void LoadGraph(FluxVisualGraph graph)
        {
            _currentGraph = graph;
            _graphView?.PopulateView(graph);
            UpdateToolbarState();
            
            if (graph != null)
            {
                EditorPrefs.SetString("FluxVisualScripting.LastGraphPath", AssetDatabase.GetAssetPath(graph));
            }
        }
        
        private void LoadLastGraph()
        {
            string lastGraphPath = EditorPrefs.GetString("FluxVisualScripting.LastGraphPath", "");
            if (!string.IsNullOrEmpty(lastGraphPath))
            {
                var graph = AssetDatabase.LoadAssetAtPath<FluxVisualGraph>(lastGraphPath);
                if (graph != null)
                {
                    LoadGraph(graph);
                }
            }
        }

        private void UpdateToolbarState()
        {
            var label = rootVisualElement?.Q<Label>("graph-name-label");
            if (label != null)
            {
                if (_currentGraph != null)
                {
                    label.text = $"📊 {_currentGraph.name}";
                    label.tooltip = AssetDatabase.GetAssetPath(_currentGraph);
                    label.style.color = new Color(0.9f, 0.9f, 0.9f);
                }
                else
                {
                    label.text = "No Graph Loaded";
                    label.tooltip = "Create a new graph or open an existing one from the 'File' menu.";
                    label.style.color = Color.gray;
                }
            }
        }

        public void OnNodeSelectionChanged(FluxNodeView nodeView)
        {
            _inspectorView?.UpdateSelection(nodeView);
        }
        
        private void OnFocus()
        {
            // Refresh the display when the window is focused, in case the graph was changed externally.
            UpdateToolbarState();
        }

        private void OnLostFocus()
        {
            // Auto-save when the user clicks away from the window.
            SaveGraph();
        }
    }
}