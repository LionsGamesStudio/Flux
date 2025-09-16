using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxGraphView : GraphView
    {
        private FluxVisualGraph _graph;
        private FluxSearchWindowProvider _searchWindowProvider;
        private readonly FluxVisualScriptingWindow _window;

        public FluxEdgeListener EdgeListener { get; private set; }

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

            EdgeListener = new FluxEdgeListener(this, _searchWindowProvider);

            nodeCreationRequest = context => {
                // When creating from the canvas, there is no origin port.
                _searchWindowProvider.Initialize(this, _window, null);
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindowProvider);
            };

            this.serializeGraphElements = OnSerializeGraphElements;
            this.unserializeAndPaste = OnUnserializeAndPaste;
            this.canPasteSerializedData = CanPaste;

            // We subscribe to the selection change event instead of overriding a method.
            graphViewChanged = OnGraphViewChanged;
            viewTransformChanged = OnViewTransformChanged;

            // We register a callback for when the mouse is released anywhere on the graph.
            this.RegisterCallback<MouseUpEvent>(evt => CheckSelectionChange());
            // We also do it for key ups, to catch selections via arrow keys or delete.
            this.RegisterCallback<KeyUpEvent>(evt => CheckSelectionChange());

            // Subscribe to the GraphDebugger events for visual debugging
            GraphDebugger.OnNodeEnter += OnNodeEnter;
            GraphDebugger.OnNodeExit += OnNodeExit;
            GraphDebugger.OnTokenTraverse += OnTokenTraverse;
            GraphDebugger.OnNodeDataUpdate += OnNodeDataUpdate;

            // To clear the debug info when stop playing
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        ~FluxGraphView()
        {
            GraphDebugger.OnNodeEnter -= OnNodeEnter;
            GraphDebugger.OnNodeExit -= OnNodeExit;
            GraphDebugger.OnTokenTraverse -= OnTokenTraverse;
            GraphDebugger.OnNodeDataUpdate -= OnNodeDataUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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

            ClearSelection();
            _window.OnElementSelected(null);

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
        
        /// <summary>
        /// A robust way to refresh the entire view. This is less surgical but more reliable
        /// than trying to refresh a single node and its edges manually.
        /// </summary>
        public void Refresh()
        {
            if (_graph == null) return;
            
            // This forces a complete reload and redraw of the entire graph from the data model.
            // It's the most reliable way to ensure the view is in sync with the data.
            PopulateView(_graph);
        }

        /// <summary>
        /// Called whenever the graph changes (nodes or edges are added/removed).
        /// </summary>
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
                        _graph.RemoveConnection(fromNodeView.Node, edge.output.name, toNodeView.Node, edge.input.name);
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
                        var fromPortData = fromNodeView.Node.OutputPorts.FirstOrDefault(p => p.Name == edge.output.name);
                        var toPortData = toNodeView.Node.InputPorts.FirstOrDefault(p => p.Name == edge.input.name);

                        if (fromPortData != null && toPortData != null)
                        {
                            _graph.AddConnection(fromPortData, fromNodeView.Node, toPortData, toNodeView.Node);
                        }
                        else
                        {
                            Debug.LogError($"[FluxGraphView] Failed to create connection. Could not find port data in the model. " +
                                           $"FromPort: '{edge.output.portName}', ToPort: '{edge.input.portName}'. This can happen if ports were rebuilt and the view is not yet updated.", _graph);
                        }
                    }
                }
            }

            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                    if (element is FluxNodeView nodeView)
                    {
                        nodeView.Node.Position = nodeView.GetPosition().position;
                        EditorUtility.SetDirty(_graph); // Mark the graph as dirty to save the new position
                    }
                }
            }

            return graphViewChange;
        }

        /// <summary>
        /// Called when the view transform (zoom/pan) changes.
        /// </summary>
        /// <param name="view"></param>
        private void OnViewTransformChanged(GraphView view)
        {
            // This can be used to save the zoom and pan state later.
        }

        /// <summary>
        /// Checks for selection changes and notifies the window.
        /// </summary>
        private void CheckSelectionChange()
        {
            // By scheduling the check for the next editor frame, we guarantee
            // that the GraphView's 'selection' property has been fully updated
            // before we read it. This solves the race condition.
            schedule.Execute(() =>
            {
                var selectedElement = selection.OfType<GraphElement>().LastOrDefault();
                _window.OnElementSelected(selectedElement);
            });
        }

        /// <summary>
        /// Determines which ports are compatible for connection when the user starts dragging a connection from a port.
        /// </summary>
        /// <param name="startPort"></param>
        /// <param name="nodeAdapter"></param>
        /// <returns></returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();

            // Get the data model of the port where the connection starts.
            var startPortData = startPort.userData as FluxNodePort;
            if (startPortData == null)
            {
                // If the start port has no data, we can't determine compatibility.
                return compatiblePorts;
            }

            // Iterate through all other ports in the graph.
            ports.ForEach(portView =>
            {
                // Basic checks: don't connect to the same node or the same port.
                if (startPort.node == portView.node || startPort == portView)
                {
                    return;
                }

                var endPortData = portView.userData as FluxNodePort;
                if (endPortData != null)
                {
                    if (startPortData.CanConnectTo(endPortData))
                    {
                        compatiblePorts.Add(portView);
                    }
                }
            });

            return compatiblePorts;
        }

        /// <summary>
        /// The single, unified method for creating a new node. If an origin port is provided,
        /// it will also attempt to create a connection.
        /// </summary>
        public FluxNodeView CreateNodeAndConnect(Type nodeLogicType, Vector2 screenPosition, Port originPort)
        {
            if (_graph == null) return null;

            var graphPosition = this.contentViewContainer.WorldToLocal(screenPosition);
            var wrapperNode = _graph.CreateNode<AttributedNodeWrapper>(graphPosition);
            wrapperNode.Initialize(nodeLogicType, _graph);

            var nodeAttr = nodeLogicType.GetCustomAttribute<FluxNodeAttribute>();
            wrapperNode.name = nodeAttr?.DisplayName ?? nodeLogicType.Name;

            EditorUtility.SetDirty(wrapperNode);

            var newNodeView = CreateNodeView(wrapperNode);

            if (originPort != null)
            {
                var targetPort = FindCompatiblePort(newNodeView, originPort);
                if (targetPort != null)
                {
                    var output = originPort.direction == Direction.Output ? originPort : targetPort;
                    var input = originPort.direction == Direction.Input ? originPort : targetPort;
                    
                    var edge = new Edge { output = output, input = input };
                    AddElement(edge);
                    
                    var change = new GraphViewChange { edgesToCreate = new List<Edge> { edge } };
                    this.graphViewChanged(change);
                }
            }
            return newNodeView;
        }

        /// <summary>
        /// Creates and adds a FluxNodeView to the GraphView for the given node data model.
        /// </summary>
        /// <param name="nodeData"></param>
        private FluxNodeView CreateNodeView(FluxNodeBase nodeData)
        {
            var nodeView = NodeViewFactory.CreateNodeView(nodeData, this);
            AddElement(nodeView);
            return nodeView;
        }

        /// <summary>
        /// Finds a port on the given node that is compatible with the origin port.
        /// </summary>
        /// <param name="nodeView"></param>
        /// <param name="originPort"></param>
        /// <returns></returns>
        private Port FindCompatiblePort(FluxNodeView nodeView, Port originPort)
        {
            var originPortData = originPort.userData as FluxNodePort;
            if (originPortData == null) return null;
            var targetContainer = originPort.direction == Direction.Input ? nodeView.outputContainer : nodeView.inputContainer;

            foreach (Port port in targetContainer.Children())
            {
                var portData = port.userData as FluxNodePort;
                if (portData != null && originPortData.CanConnectTo(portData))
                {
                    return port;
                }
            }
            return null;
        }

        /// <summary>
        /// A helper method to find the data model object for a visual edge.
        /// </summary>
        public FluxNodeConnection GetConnectionDataForEdge(Edge edge)
        {
            var fromNodeView = edge.output.node as FluxNodeView;
            var toNodeView = edge.input.node as FluxNodeView;

            if (fromNodeView == null || toNodeView == null) return null;

            return _graph.Connections.FirstOrDefault(c =>
                c.FromNodeId == fromNodeView.Node.NodeId && c.FromPortName == edge.output.portName &&
                c.ToNodeId == toNodeView.Node.NodeId && c.ToPortName == edge.input.portName);
        }

        #region Visual Debugging Support
        
        /// <summary>
        /// Called when a node begins execution.
        /// </summary>
        /// <param name="nodeId"></param>
        private void OnNodeEnter(string nodeId)
        {
            var nodeView = GetNodeByGuid(nodeId) as FluxNodeView;
            nodeView?.Flash(Color.yellow);
        }

        /// <summary>
        /// Called when a node finishes execution.
        /// </summary>
        /// <param name="nodeId"></param>
        private void OnNodeExit(string nodeId)
        {
            // Optional: Could add a different flash color for exit.
        }

        /// <summary>
        /// Called when an execution token traverses a connection.
        /// </summary>
        /// <param name="fromNodeId"></param>
        /// <param name="fromPortName"></param>
        /// <param name="toNodeId"></param>
        /// <param name="toPortName"></param>
        private void OnTokenTraverse(string fromNodeId, string fromPortName, string toNodeId, string toPortName)
        {
            var edge = edges.ToList().FirstOrDefault(e =>
            {
                var fromNode = e.output.node as FluxNodeView;
                var toNode = e.input.node as FluxNodeView;
                return fromNode?.Node.NodeId == fromNodeId && e.output.portName == fromPortName &&
                    toNode?.Node.NodeId == toNodeId && e.input.portName == toPortName;
            });

            edge?.Flash(Color.cyan);
        }

        /// <summary>
        /// Called when a node updates its data ports during execution.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="portValues"></param>
        private void OnNodeDataUpdate(string nodeId, Dictionary<string, string> portValues)
        {
            var nodeView = GetNodeByGuid(nodeId) as FluxNodeView;
            nodeView?.SetPortDebugValues(portValues);
        }
        
        /// <summary>
        /// Called when the play mode state changes.
        /// </summary>
        /// <param name="state"></param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // When we exit play mode, clear all the debug labels.
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                foreach (var nodeView in nodes.OfType<FluxNodeView>())
                {
                    nodeView.ClearPortDebugValues();
                }
            }
        }


        #endregion

        #region Copy/Paste Support

        /// <summary>
        /// This method is called by the GraphView when the user presses Ctrl+C or selects "Copy".
        /// It now serializes both the selected nodes and the connections between them.
        /// </summary>
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var selectedNodeViews = elements.OfType<FluxNodeView>().ToList();
            if (!selectedNodeViews.Any())
            {
                return string.Empty;
            }

            var clipboardData = new ClipboardData();

            // 1. Create a set of the selected node IDs for quick lookups.
            var selectedNodeIds = new HashSet<string>(selectedNodeViews.Select(v => v.Node.NodeId));

            // 2. Serialize all selected nodes.
            foreach (var nodeView in selectedNodeViews)
            {
                clipboardData.NodesJson.Add(EditorJsonUtility.ToJson(nodeView.Node));
            }

            // 3. Find and serialize all connections that are *internal* to the selection.
            foreach (var connection in _graph.Connections)
            {
                // A connection is internal if both its source and destination nodes are in our selection set.
                if (selectedNodeIds.Contains(connection.FromNodeId) && selectedNodeIds.Contains(connection.ToNodeId))
                {
                    clipboardData.Connections.Add(new ConnectionData
                    {
                        FromNodeId = connection.FromNodeId,
                        FromPortName = connection.FromPortName,
                        ToNodeId = connection.ToNodeId,
                        ToPortName = connection.ToPortName
                    });
                }
            }

            return JsonUtility.ToJson(clipboardData);
        }

        /// <summary>
        /// This method is called by the GraphView when the user presses Ctrl+V or selects "Paste".
        /// It now recreates both the nodes and their internal connections.
        /// </summary>
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            if (_graph == null) return;

            var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
            if (clipboardData == null) return;

            // Start a single Undo group for the entire paste operation.
            Undo.RecordObject(_graph, "Paste Graph Elements");

            var newNodes = new List<FluxNodeView>();
            // This dictionary is crucial to remap old connections to the newly created nodes.
            var oldIdToNewNodeMap = new Dictionary<string, FluxNodeBase>();

            // --- 1. Recreate all the nodes ---
            foreach (var nodeJson in clipboardData.NodesJson)
            {
                var tempNode = ScriptableObject.CreateInstance<AttributedNodeWrapper>();
                EditorJsonUtility.FromJsonOverwrite(nodeJson, tempNode);
                
                string oldNodeId = tempNode.NodeId;
                
                // Create a new node in the graph asset. It will get a NEW, unique ID.
                var newNode = _graph.CreateNode<AttributedNodeWrapper>(tempNode.Position + new Vector2(50, 50));

                // Initialize its logic and copy the data.
                var logicType = tempNode.NodeLogic.GetType();
                newNode.Initialize(logicType, _graph);
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(tempNode.NodeLogic), newNode.NodeLogic);
                newNode.name = tempNode.name;
                
                // Store the mapping from the old ID to the new node instance.
                oldIdToNewNodeMap[oldNodeId] = newNode;
                
                var nodeView = CreateNodeView(newNode);
                newNodes.Add(nodeView);
                
                ScriptableObject.DestroyImmediate(tempNode, true);
            }

            // --- 2. Recreate all the connections ---
            foreach (var connectionData in clipboardData.Connections)
            {
                // Find the newly created source and destination nodes using our map.
                if (oldIdToNewNodeMap.TryGetValue(connectionData.FromNodeId, out var newFromNode) &&
                    oldIdToNewNodeMap.TryGetValue(connectionData.ToNodeId, out var newToNode))
                {
                    // Find the corresponding port data on the new nodes.
                    var fromPortData = newFromNode.OutputPorts.FirstOrDefault(p => p.Name == connectionData.FromPortName);
                    var toPortData = newToNode.InputPorts.FirstOrDefault(p => p.Name == connectionData.ToPortName);
                    
                    if (fromPortData != null && toPortData != null)
                    {
                        // Add the connection to our data model.
                        _graph.AddConnection(fromPortData, newFromNode, toPortData, newToNode);
                    }
                }
            }

            // Repopulate the entire view to draw the new connections.
            PopulateView(_graph);
            
            // Select all the newly pasted nodes.
            ClearSelection();
            newNodes.ForEach(AddToSelection);
        }

        /// <summary>
        /// This method tells the GraphView whether the current clipboard content is something we can paste.
        /// </summary>
        private bool CanPaste(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return false;
                
                var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
                
                return clipboardData != null && clipboardData.NodesJson.Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// A helper class to structure our clipboard data for JSON serialization.
        /// </summary>
        [Serializable]
        public class ClipboardData
        {
            // We will store the full JSON of each node.
            public List<string> NodesJson = new List<string>();
            
            // We will store a serializable version of the connections.
            public List<ConnectionData> Connections = new List<ConnectionData>();
        }

        /// <summary>
        /// A simple, serializable representation of a FluxNodeConnection.
        /// </summary>
        [Serializable]
        public struct ConnectionData
        {
            public string FromNodeId;
            public string FromPortName;
            public string ToNodeId;
            public string ToPortName;
        }
        
        #endregion
    }
}