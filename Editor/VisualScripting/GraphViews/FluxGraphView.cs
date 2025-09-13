using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.VisualScripting.Editor.NodeViews;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// The main GraphView for displaying and editing Flux visual script graphs.
    /// It handles node creation, connection, serialization, and synchronization with the underlying graph asset.
    /// </summary>
    public class FluxGraphView : GraphView
    {
        /// <summary> This factory is required by UI Toolkit to instantiate the GraphView from UXML files. </summary>
        public new class UxmlFactory : UxmlFactory<FluxGraphView, GraphView.UxmlTraits> { }

        private FluxVisualScriptingWindow _window;
        private FluxVisualGraph _graph;
        private FluxSearchWindowProvider _searchWindowProvider;
        private static readonly Dictionary<Type, Type> NodeViewMapping = new Dictionary<Type, Type>();

        /// <summary> A reference to the parent editor window that hosts this view. </summary>
        public FluxVisualScriptingWindow Window => _window;

        /// <summary> A reference to the underlying graph asset being edited. </summary>
        public FluxVisualGraph Graph => _graph;

        /// <summary> Default constructor used by UI Toolkit when creating the view from UXML. </summary>
        public FluxGraphView() { InitializeGraphView(); }
        
        /// <summary> Constructor used when creating the view manually from C# code. </summary>
        public FluxGraphView(FluxVisualScriptingWindow window) { _window = window; InitializeGraphView(); }

        /// <summary> Sets up the initial state and manipulators for the graph view. </summary>
        private void InitializeGraphView()
        {
            style.flexGrow = 1;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            _searchWindowProvider = ScriptableObject.CreateInstance<FluxSearchWindowProvider>();
            _searchWindowProvider.Initialize(this, _window);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindowProvider);
            
            serializeGraphElements = OnSerializeGraphElements;
            canPasteSerializedData = CanPaste;
            unserializeAndPaste = OnUnserializeAndPaste;
        }

        /// <summary> Clears the current view and repopulates it with nodes and connections from a given graph asset. </summary>
        public void PopulateView(FluxVisualGraph graph)
        {
            _graph = graph;
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (graph == null) return;
            foreach (var node in graph.Nodes) { CreateNodeView(node); }
            foreach (var connection in graph.Connections)
            {
                var fromNodeView = FindNodeView(connection.FromNode);
                var toNodeView = FindNodeView(connection.ToNode);
                if (fromNodeView != null && toNodeView != null)
                {
                    var fromPort = fromNodeView.GetOutputPort(connection.FromPort);
                    var toPort = toNodeView.GetInputPort(connection.ToPort);
                    if (fromPort != null && toPort != null)
                    {
                        var edge = fromPort.ConnectTo(toPort);
                        AddElement(edge);
                    }
                }
            }
        }
        
        /// <summary> Factory method to create a visual representation (a FluxNodeView) for a given data node. </summary>
        private FluxNodeView CreateNodeView(FluxNodeBase node)
        {
            FluxNodeView nodeView;

            // The constructor for a node view must receive the DATA MODEL (_graph), not the UI VIEW (this).
            if (NodeViewMapping.TryGetValue(node.GetType(), out var viewType))
            {
                nodeView = (FluxNodeView)Activator.CreateInstance(viewType, new object[] { _graph, node });
            }
            else
            {
                nodeView = new FluxNodeView(_graph, node);
            }

            AddElement(nodeView);
            return nodeView;
        }

        /// <summary> An override that determines which ports can be connected to a given start port. </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startFluxPort = startPort.userData as FluxNodePort;
            if (startFluxPort == null) return compatiblePorts;

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node)
                {
                    var endFluxPort = port.userData as FluxNodePort;
                    if (endFluxPort != null && startFluxPort.CanConnectTo(endFluxPort))
                    {
                        compatiblePorts.Add(port);
                    }
                }
            });
            return compatiblePorts;
        }
        
        /// <summary> The central callback that syncs changes made in the view (UI) back to the graph asset (data model). </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_graph == null) return graphViewChange;
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var element in graphViewChange.elementsToRemove)
                {
                    if (element is FluxNodeView nodeView) _graph.RemoveNode(nodeView.Node);
                    else if (element is Edge edge)
                    {
                        var fromNodeView = edge.output.node as FluxNodeView;
                        var toNodeView = edge.input.node as FluxNodeView;
                        if (fromNodeView != null && toNodeView != null)
                        {
                            _graph.RemoveConnection(fromNodeView.Node, edge.output.portName, toNodeView.Node, edge.input.portName);
                        }
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
                        _graph.CreateConnection(fromNodeView.Node, edge.output.portName, toNodeView.Node, edge.input.portName);
                    }
                }
            }
            if (graphViewChange.movedElements != null)
            {
                foreach(var element in graphViewChange.movedElements)
                {
                    if(element is FluxNodeView nodeView)
                    {
                        Vector2 newPosition = nodeView.GetPosition().position;
                        _graph.SetNodePosition(nodeView.Node, newPosition);
                    }
                }
            }
            EditorUtility.SetDirty(_graph);
            return graphViewChange;
        }

        /// <summary> Creates a new node at a specific screen position, handling coordinate conversion. </summary>
        public void CreateNodeAtScreenPosition(Type nodeType, Vector2 screenPosition)
        {
            var graphPosition = this.contentViewContainer.WorldToLocal(screenPosition);
            CreateNode(nodeType, graphPosition);
        }

        /// <summary> Creates a new node asset, adds it to the graph, and creates its visual representation. </summary>
        public void CreateNode(Type nodeType, Vector2 graphPosition)
        {
            if (_graph == null) return;
            var node = ScriptableObject.CreateInstance(nodeType) as FluxNodeBase;
            node.name = nodeType.Name;
            AssetDatabase.AddObjectToAsset(node, _graph);
            _graph.AddNode(node);
            _graph.SetNodePosition(node, graphPosition);
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            CreateNodeView(node);
        }

        /// <summary> Override for the right-click context menu. </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { base.BuildContextualMenu(evt); }
        
        /// <summary> Notifies the parent window that the node selection has changed. </summary>
        public void OnNodeSelectionChanged(FluxNodeView nodeView) => _window?.OnNodeSelectionChanged(nodeView);
        
        /// <summary> Helper to find the visual view for a given data node. </summary>
        private FluxNodeView FindNodeView(FluxNodeBase node) => GetNodeByGuid(node.NodeId) as FluxNodeView;

        #region Copy Paste Logic

        [Serializable] private class ClipboardData { public List<SerializedNode> Nodes = new List<SerializedNode>(); public List<SerializedConnection> Connections = new List<SerializedConnection>(); }
        [Serializable] private class SerializedNode { public string OriginalNodeId; public string NodeJson; public Vector2 Position; }
        [Serializable] private class SerializedConnection { public string FromNodeId; public string FromPortName; public string ToNodeId; public string ToPortName; }
        
        /// <summary> Callback used by the GraphView to determine if the "Paste" action should be enabled. </summary>
        private bool CanPaste(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return false;
                var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
                return clipboardData != null && clipboardData.Nodes.Any();
            }
            catch { return false; }
        }

        /// <summary> Callback used by the GraphView to execute the "Paste" action. </summary>
        private void OnUnserializeAndPaste(string operationName, string data)
        {
            // We use the 'data' parameter passed by the GraphView. This is the source of truth for the operation.
            var clipboardData = JsonUtility.FromJson<ClipboardData>(data);
            if (clipboardData == null) return;

            ClearSelection();

            var newNodesMapping = new Dictionary<string, FluxNodeBase>();
            var newViewsToSelect = new List<ISelectable>();
            var basePosition = clipboardData.Nodes.FirstOrDefault()?.Position ?? Vector2.zero;
            
            // Determine the paste position (center of view for Paste, offset for Duplicate)
            Vector2 pasteCenter;
            if (operationName == "Paste")
            {
                pasteCenter = contentViewContainer.WorldToLocal(worldBound.center);
            }
            else // "Duplicate"
            {
                var firstOriginalNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == clipboardData.Nodes[0]?.OriginalNodeId);
                pasteCenter = firstOriginalNode?.Position ?? Vector2.zero;
            }


            foreach (var serializedNode in clipboardData.Nodes)
            {
                var originalNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == serializedNode.OriginalNodeId);
                if (originalNode == null) continue;

                var newNode = _graph.DuplicateNode(originalNode);
                JsonUtility.FromJsonOverwrite(serializedNode.NodeJson, newNode);
                
                // Position pasted nodes intelligently
                newNode.Position = pasteCenter + (serializedNode.Position - basePosition) + new Vector2(30,30);

                newNodesMapping.Add(serializedNode.OriginalNodeId, newNode);
                var nodeView = CreateNodeView(newNode);
                newViewsToSelect.Add(nodeView);
            }

            foreach (var serializedConn in clipboardData.Connections)
            {
                if (newNodesMapping.TryGetValue(serializedConn.FromNodeId, out var fromNode) &&
                    newNodesMapping.TryGetValue(serializedConn.ToNodeId, out var toNode))
                {
                    _graph.CreateConnection(fromNode, serializedConn.FromPortName, toNode, serializedConn.ToPortName);
                }
            }
            
            PopulateView(_graph);
            newViewsToSelect.ForEach(AddToSelection);
        }
        
        /// <summary> Callback used by the GraphView to execute the "Copy" action. </summary>
        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var selectedNodeViews = elements.OfType<FluxNodeView>().ToList();
            if (!selectedNodeViews.Any()) return "";

            var selectedNodeGuids = new HashSet<string>(selectedNodeViews.Select(v => v.Node.NodeId));
            var clipboardData = new ClipboardData();
            var basePosition = selectedNodeViews.First().Node.Position;

            foreach (var nodeView in selectedNodeViews)
            {
                var node = nodeView.Node;
                clipboardData.Nodes.Add(new SerializedNode
                {
                    OriginalNodeId = node.NodeId,
                    NodeJson = JsonUtility.ToJson(node),
                    Position = node.Position - basePosition
                });
            }

            foreach (var nodeView in selectedNodeViews)
            {
                var connections = _graph.GetOutputConnections(nodeView.Node);
                foreach(var connection in connections)
                {
                    if (selectedNodeGuids.Contains(connection.ToNode.NodeId))
                    {
                        clipboardData.Connections.Add(new SerializedConnection
                        {
                            FromNodeId = connection.FromNode.NodeId,
                            FromPortName = connection.FromPort,
                            ToNodeId = connection.ToNode.NodeId,
                            ToPortName = connection.ToPort
                        });
                    }
                }
            }

            var json = JsonUtility.ToJson(clipboardData);
            GUIUtility.systemCopyBuffer = json;
            return json;
        }

        #endregion
    }
}