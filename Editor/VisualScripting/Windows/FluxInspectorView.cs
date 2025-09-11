using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// The inspector panel within the Visual Scripting Editor window. It displays the properties
    /// and actions for the currently selected node.
    /// </summary>
    public class FluxInspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<FluxInspectorView, VisualElement.UxmlTraits> { }
        
        private VisualElement _contentContainer;

        public FluxInspectorView()
        {
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 1f);

            var header = new Label("Node Inspector") { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10, backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f) }};
            Add(header);

            var scrollView = new ScrollView { style = { flexGrow = 1 } };
            Add(scrollView);

            _contentContainer = new VisualElement { style = { paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10 }};
            scrollView.Add(_contentContainer);

            ShowNoSelection();
        }
        
        public void UpdateSelection(FluxNodeView nodeView)
        {
            _contentContainer.Clear();
            if (nodeView == null)
            {
                ShowNoSelection();
                return;
            }
            ShowNodeInspector(nodeView);
        }

        private void ShowNoSelection()
        {
            var label = new Label("No Node Selected") { style = { alignSelf = Align.Center, marginTop = 50, opacity = 0.7f } };
            _contentContainer.Add(label);
        }
        
        private void ShowNodeInspector(FluxNodeView nodeView)
        {
            var node = nodeView.Node;
            var serializedNode = new SerializedObject(node);
            
            var inspectorElement = new InspectorElement(node);
            _contentContainer.Add(inspectorElement);
            
            AddSeparator();
            
            ShowNodeInfo(serializedNode, nodeView);
            ShowNodeActions(nodeView.Graph, node);
        }
        
        private void ShowNodeInfo(SerializedObject serializedNode, FluxNodeView nodeView)
        {
            var node = (FluxNodeBase)serializedNode.targetObject;
            var infoLabel = new Label("Node Information") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, marginTop = 10 } };
            _contentContainer.Add(infoLabel);

            var idField = new TextField("Node ID") { value = node.NodeId };
            idField.SetEnabled(false);
            _contentContainer.Add(idField);

            var positionField = new Vector2Field("Position");
            positionField.BindProperty(serializedNode.FindProperty("_position"));
            positionField.RegisterValueChangedCallback(evt =>
            {
                nodeView.SetPosition(new Rect(evt.newValue, Vector2.zero));
            });
            _contentContainer.Add(positionField);
        }
        
        private void ShowNodeActions(FluxVisualGraph graph, FluxNodeBase node)
        {
            var actionsLabel = new Label("Actions") { style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, marginTop = 10 } };
            _contentContainer.Add(actionsLabel);

            var validateButton = new Button(() =>
            {
                bool isValid = node.Validate();
                string message = isValid ? "Node is valid." : "Node validation failed (check required inputs).";
                EditorUtility.DisplayDialog("Node Validation", message, "OK");
            }) { text = "Validate Node" };
            _contentContainer.Add(validateButton);

            var executeButton = new Button(() =>
            {
                // Use FindObjectOfType for broader Unity version compatibility
                var runner = GameObject.FindObjectOfType<FluxVisualScriptComponent>();
                if (runner != null && runner.Graph == graph && runner.LastExecutor != null)
                {
                    var outputs = runner.LastExecutor.ExecuteSingleNodeForDebug(node);
                    string outputInfo = outputs.Any() 
                        ? string.Join("\n", outputs.Select(kvp => $"• {kvp.Key}: {kvp.Value}"))
                        : "No data outputs.";
                    EditorUtility.DisplayDialog($"'{node.NodeName}' Executed", $"Outputs:\n\n{outputInfo}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Execution Error", "Could not find an active runner for this graph in the scene. Please press Play and execute the graph from its component once before using this debug feature.", "OK");
                }
            }) { text = "Execute Node (Debug)" };
            
            executeButton.SetEnabled(Application.isPlaying);
            _contentContainer.Add(executeButton);

            var selectButton = new Button(() =>
            {
                Selection.activeObject = node;
                EditorGUIUtility.PingObject(node);
            }) { text = "Select Asset in Project" };
            _contentContainer.Add(selectButton);
        }

        private void AddSeparator()
        {
            var separator = new VisualElement { style = { height = 1, backgroundColor = new Color(0.12f, 0.12f, 0.12f), marginTop = 15, marginBottom = 10 } };
            _contentContainer.Add(separator);
        }
    }
}