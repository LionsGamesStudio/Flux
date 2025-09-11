using System.Linq;
using UnityEngine;
using UnityEditor;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// Custom inspector for FluxVisualGraph assets. Provides an "Open in Editor" button
    /// and displays useful statistics and actions for the graph.
    /// </summary>
    [CustomEditor(typeof(FluxVisualGraph))]
    [CanEditMultipleObjects]
    public class FluxVisualGraphEditor : UnityEditor.Editor
    {
        private FluxVisualGraph _graph;
        private FluxVisualGraph[] _graphs;

        private void OnEnable()
        {
            _graph = (FluxVisualGraph)target;
            _graphs = targets.Cast<FluxVisualGraph>().ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool isMultiSelection = _graphs.Length > 1;
            
            // --- Header ---
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            
            if (isMultiSelection)
            {
                EditorGUILayout.LabelField($"📊 {_graphs.Length} Graphs Selected", headerStyle);
            }
            else
            {
                EditorGUILayout.LabelField($"📊 {_graph.name}", headerStyle);
            }
            EditorGUILayout.Space();

            // --- Primary Action Button ---
            if (!isMultiSelection)
            {
                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f); // A nice blue
                if (GUILayout.Button("🔧 Open in Visual Editor", GUILayout.Height(40)))
                {
                    OpenInVisualEditor();
                }
                GUI.backgroundColor = Color.white;
            }

            // --- Secondary Action Buttons ---
            EditorGUILayout.BeginHorizontal();
            {
                // Validate Button
                GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Orange/Yellow
                if (GUILayout.Button("✓ Validate"))
                {
                    ValidateWithFeedback();
                }

                // Show Statistics Button
                GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f); // Green
                if (GUILayout.Button("📈 Show Stats"))
                {
                    ShowStatistics();
                }

                // Test Execute Button
                GUI.backgroundColor = new Color(0.9f, 0.5f, 0.5f); // Reddish
                GUI.enabled = Application.isPlaying && !isMultiSelection;
                if (GUILayout.Button("▶ Test Execute"))
                {
                    TestExecuteInEditor();
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // Separator line
            EditorGUILayout.Space();

            // --- Default Inspector Properties ---
            EditorGUILayout.LabelField("Graph Properties", EditorStyles.boldLabel);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }

        private void OpenInVisualEditor()
        {
            var window = EditorWindow.GetWindow<FluxVisualScriptingWindow>("Flux Visual Scripting");
            window.LoadGraph(_graph);
        }

        private void ValidateWithFeedback()
        {
            bool allValid = true;
            string failedGraphs = "";
            foreach (var graph in _graphs)
            {
                if (!graph.Validate())
                {
                    allValid = false;
                    failedGraphs += $"\n• {graph.name}";
                }
            }

            string title = allValid ? "Validation Passed" : "Validation Failed";
            string message = allValid ?
                $"Successfully validated {_graphs.Length} graph(s)." :
                $"One or more graphs failed validation:{failedGraphs}\n\nCheck the console for details.";
            
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private void ShowStatistics()
        {
            // This method can show stats for single or multiple selections
            var totalNodes = _graphs.Sum(g => g.Nodes.Count);
            var totalConnections = _graphs.Sum(g => g.Connections.Count);
            var allNodes = _graphs.SelectMany(g => g.Nodes);
            var nodeTypes = allNodes.GroupBy(n => n.GetType().Name)
                .Select(g => $"  • {g.Key}: {g.Count()}")
                .OrderBy(s => s);
            
            string title = _graphs.Length > 1 ? $"Combined Statistics for {_graphs.Length} Graphs" : $"Statistics for '{_graph.name}'";

            Debug.Log($"--- {title} ---\n" +
                      $"Total Nodes: {totalNodes}\n" +
                      $"Total Connections: {totalConnections}\n" +
                      $"Node Types:\n{string.Join("\n", nodeTypes)}\n" +
                      $"--------------------", 
                      target); // Clicking the log will ping the asset(s)
        }

        private void TestExecuteInEditor()
        {
            // Find an existing runner or create a temporary one for the test.
            var runner = FindObjectOfType<FluxVisualScriptComponent>();
            if (runner != null && runner.Graph == _graph)
            {
                 Debug.Log($"Executing graph using existing component '{runner.gameObject.name}'.", runner.gameObject);
                 runner.ExecuteGraph();
            }
            else
            {
                Debug.Log("No specific component found. Creating a temporary runner to execute the graph for testing purposes.", _graph);
                var tempRunnerGO = new GameObject($"__TempGraphRunner_{_graph.name}");
                var tempRunnerComp = tempRunnerGO.AddComponent<FluxVisualScriptComponent>();
                tempRunnerComp.Graph = _graph;
                tempRunnerComp.ExecuteGraph();
            }
        }
    }
}