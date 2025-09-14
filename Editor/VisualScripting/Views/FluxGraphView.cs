using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FluxFramework.VisualScripting;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxGraphView : GraphView
    {
        private FluxVisualGraph _graph;
        private FluxSearchWindowProvider _searchWindowProvider;
        private readonly FluxVisualScriptingWindow _window;

        public FluxGraphView(FluxVisualScriptingWindow window)
        {
            _window = window;

            // Add background grid
            Insert(0, new GridBackground());

            // Add manipulators for basic interactions
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // This is required to draw the lines for new connections
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.visualscripting/Editor/Application/Graphs/Styles/GraphView.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning("[FluxGraphView] Could not find the default Visual Scripting stylesheet. Connection lines may be invisible.");
            }

            _searchWindowProvider = ScriptableObject.CreateInstance<FluxSearchWindowProvider>();
            _searchWindowProvider.Initialize(this, _window);

            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindowProvider);

            this.serializeGraphElements = OnSerializeGraphElements;
            this.unserializeAndPaste = OnUnserializeAndPaste;
            this.canPasteSerializedData = CanPaste;
        }

        /// <summary>
        /// Clears the view and populates it with the nodes and connections from a graph asset.
        /// </summary>
        public void PopulateView(FluxVisualGraph graph)
        {
            _graph = graph;

            // This is required to make sure we don't keep old elements when loading a new graph
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (_graph == null) return;

            // Create a view for each node in the data model
            foreach (var nodeData in _graph.Nodes)
            {
                CreateNodeView(nodeData);
            }

            // Create edges for each connection in the data model
            foreach (var connectionData in _graph.Connections)
            {
                var fromNodeView = GetNodeByGuid(connectionData.FromNodeId) as FluxNodeView;
                var toNodeView = GetNodeByGuid(connectionData.ToNodeId) as FluxNodeView;

                if (fromNodeView != null && toNodeView != null)
                {
                    var fromPortView = fromNodeView.outputContainer.Q<Port>(connectionData.FromPortName);
                    var toPortView = toNodeView.inputContainer.Q<Port>(connectionData.ToPortName);

                    if (fromPortView != null && toPortView != null)
                    {
                        var edge = fromPortView.ConnectTo(toPortView);
                        AddElement(edge);
                    }
                }
            }
        }

        // This is the main callback that synchronizes UI changes back to our data model (_graph).
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is FluxNodeView nodeView)
                    {
                        _graph.DeleteNode(nodeView.Node);
                    }
                    if (element is Edge edge)
                    {
                        var fromNodeView = edge.output.node as FluxNodeView;
                        var toNodeView = edge.input.node as FluxNodeView;
                        _graph.RemoveConnection(fromNodeView.Node, edge.output.portName, toNodeView.Node, edge.input.portName);
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var fromNodeView = edge.output.node as FluxNodeView;
                    var toNodeView = edge.input.node as FluxNodeView;

                    if (fromNodeView != null && toNodeView != null)
                    {
                        // TODO: This part is tricky because the PortView doesn't store our FluxNodePort.
                        // We'll need to look it up by name.
                        var fromPortData = fromNodeView.Node.OutputPorts.First(p => p.Name == edge.output.portName);
                        var toPortData = toNodeView.Node.InputPorts.First(p => p.Name == edge.input.portName);

                        _graph.AddConnection(fromPortData, fromNodeView.Node, toPortData, toNodeView.Node);
                    }
                }
            }

            return graphViewChange;
        }

        // We override this to define our own connection rules.
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        /// <summary>
        /// Creates a new node of the specified logic type at the given screen position.
        /// </summary>
        /// <param name="nodeLogicType"></param>
        /// <param name="screenPosition"></param>
        public void CreateAttributedNode(Type nodeLogicType, Vector2 screenPosition)
        {
            if (_graph == null)
            {
                EditorUtility.DisplayDialog("No Graph Selected", "Cannot create a node because no graph asset is loaded.", "OK");
                return;
            }

            // Record the entire operation so it can be undone.
            Undo.RecordObject(_graph, "Create Node");

            var graphPosition = this.contentViewContainer.WorldToLocal(screenPosition);

            // Create the wrapper node and add it as a sub-asset to the graph.
            var wrapperNode = _graph.CreateNode<AttributedNodeWrapper>(graphPosition);

            // Initialize the wrapper with the selected logic. This also generates its ports.
            wrapperNode.Initialize(nodeLogicType);

            // Give the node a user-friendly name in the asset hierarchy.
            var nodeAttr = nodeLogicType.GetCustomAttribute<FluxNodeAttribute>();
            wrapperNode.name = nodeAttr?.DisplayName ?? nodeLogicType.Name;

            // Mark the wrapper as dirty to ensure its changes (like the new ports) are saved.
            EditorUtility.SetDirty(wrapperNode);

            // Create the visual representation for the new node in the graph view.
            CreateNodeView(wrapperNode);
        }

        /// <summary>
        /// Creates and adds a FluxNodeView to the GraphView for the given node data model.
        /// </summary>
        /// <param name="nodeData"></param>
        private void CreateNodeView(FluxNodeBase nodeData)
        {
            var nodeView = new FluxNodeView(nodeData);
            AddElement(nodeView);
        }

        private void CreateNode(System.Type type, Vector2 position)
        {
            if (_graph != null)
            {
                var node = _graph.CreateNode<AttributedNodeWrapper>(position);

                // TODO: For now, we need a way to create an example INode to test.
                // We will create a dummy 'AddNode' for this.
                var addNodeLogicType = System.Type.GetType("FluxFramework.VisualScripting.Node.AddNode, Assembly-CSharp");
                if (addNodeLogicType != null)
                {
                    node.Initialize(addNodeLogicType);
                }

                CreateNodeView(node);
            }
        }

        #region Copy/Paste Support

        /// <summary>
        /// This method is called by the GraphView when the user presses Ctrl+C or selects "Copy".
        /// It should return a string representation of the selected elements. We'll use JSON.
        /// </summary>
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var data = new ClipboardData();

            // Store the nodes
            foreach (var nodeView in elements.OfType<FluxNodeView>())
            {
                data.Nodes.Add(JsonUtility.ToJson(nodeView.Node));
            }

            // TODO: Store the connections between the selected nodes

            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// This method is called by the GraphView when the user presses Ctrl+V or selects "Paste".
        /// It receives the string from the clipboard and should create new nodes.
        /// </summary>
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            if (_graph == null) return;

            var clipboardData = JsonUtility.FromJson<ClipboardData>(data);

            Undo.RecordObject(_graph, "Paste Nodes");

            foreach (var nodeJson in clipboardData.Nodes)
            {
                // This is a bit tricky. We need to create an empty ScriptableObject first,
                // then overwrite it with our JSON data.
                var tempNode = ScriptableObject.CreateInstance<AttributedNodeWrapper>();
                JsonUtility.FromJsonOverwrite(nodeJson, tempNode);

                // Now, create a *real* node in our graph asset by cloning the temporary one.
                var newNode = _graph.CreateNode<AttributedNodeWrapper>(tempNode.Position + new Vector2(30, 30));

                // We need to re-initialize the logic from the type, because SerializeReference isn't perfect with FromJsonOverwrite
                var logicType = Type.GetType(tempNode.NodeLogic.GetType().AssemblyQualifiedName);
                newNode.Initialize(logicType);

                // Now copy the serialized fields from the pasted logic to the new logic instance
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(tempNode.NodeLogic), newNode.NodeLogic);

                CreateNodeView(newNode);

                // Clean up the temporary node
                ScriptableObject.DestroyImmediate(tempNode);
            }
        }

        /// <summary>
        /// This method tells the GraphView whether the current clipboard content is something we can paste.
        /// </summary>
        private bool CanPaste(string data)
        {
            try
            {
                var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
                return clipboardData != null && clipboardData.Nodes.Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A helper class to structure our clipboard data for JSON serialization.
        /// </summary>
        public class ClipboardData
        {
            public List<string> Nodes = new List<string>();
            // We will add connections here later
        }
        
        #endregion
    }
}