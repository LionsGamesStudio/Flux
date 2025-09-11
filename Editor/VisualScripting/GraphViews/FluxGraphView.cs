using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.VisualScripting.Editor.NodeViews;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// The main GraphView for displaying and editing Flux visual script graphs.
    /// It handles node creation, connection, and synchronization with the underlying graph asset.
    /// </summary>
    public class FluxGraphView : GraphView
    {
        // This factory is required by UI Toolkit to instantiate the GraphView from UXML.
        public new class UxmlFactory : UxmlFactory<FluxGraphView, GraphView.UxmlTraits> { }

        private FluxVisualScriptingWindow _window;
        private FluxVisualGraph _graph;
        private FluxSearchWindowProvider _searchWindowProvider;
        
        // A mapping to associate specific node data types with custom view classes.
        // This could be populated dynamically using reflection in a more advanced system.
        private static readonly Dictionary<Type, Type> NodeViewMapping = new Dictionary<Type, Type>();

        public FluxVisualScriptingWindow Window => _window;
        public FluxVisualGraph Graph => _graph;

        public FluxGraphView()
        {
            // This constructor is for UI Toolkit's instantiation.
            // The Initialize method will be called later to link it to the window.
            InitializeGraphView();
        }

        public FluxGraphView(FluxVisualScriptingWindow window)
        {
            _window = window;
            InitializeGraphView();
        }

        private void InitializeGraphView()
        {
            style.flexGrow = 1;
            
            // Enable standard graph manipulations
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // Set up the node creation menu (Search Window)
            _searchWindowProvider = ScriptableObject.CreateInstance<FluxSearchWindowProvider>();
            _searchWindowProvider.Initialize(this);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindowProvider);
        }

        /// <summary>
        /// Populates the view with nodes and connections from a graph asset.
        /// </summary>
        public void PopulateView(FluxVisualGraph graph)
        {
            _graph = graph;
            graphViewChanged -= OnGraphViewChanged; // Unsubscribe previous listener
            DeleteElements(graphElements); // Clear the old graph
            graphViewChanged += OnGraphViewChanged; // Subscribe new listener

            if (graph == null) return;

            // Create visual nodes for each data node in the graph asset.
            foreach (var node in graph.Nodes)
            {
                CreateNodeView(node);
            }

            // Create visual edges for each connection in the graph asset.
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
        
        private FluxNodeView CreateNodeView(FluxNodeBase node)
        {
            FluxNodeView nodeView;
            
            if (NodeViewMapping.TryGetValue(node.GetType(), out var viewType))
            {
                nodeView = (FluxNodeView)Activator.CreateInstance(viewType, new object[] { this.Graph, node });
            }
            else
            {
                nodeView = new FluxNodeView(this.Graph, node);
            }
            
            AddElement(nodeView);
            return nodeView;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startFluxPort = startPort.userData as FluxNodePort;

            if(startFluxPort == null) return compatiblePorts;

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
        
        /// <summary>
        /// This callback is the core of the editor. It syncs changes made in the view (UI)
        /// back to the graph asset (data model).
        /// </summary>
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (_graph == null) return graphViewChange;

            // Handle deleted elements
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
                            var connection = _graph.Connections.FirstOrDefault(c =>
                                c.FromNode.NodeId == fromNodeView.Node.NodeId && c.FromPort == edge.output.name &&
                                c.ToNode.NodeId == toNodeView.Node.NodeId && c.ToPort == edge.input.name);
                            if (connection != null) _graph.RemoveConnection(connection);
                        }
                    }
                }
            }

            // Handle created edges
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var fromNodeView = edge.output.node as FluxNodeView;
                    var toNodeView = edge.input.node as FluxNodeView;
                    if (fromNodeView != null && toNodeView != null)
                    {
                        _graph.CreateConnection(fromNodeView.Node, edge.output.name, toNodeView.Node, edge.input.name);
                    }
                }
            }
            
            // Handle moved elements
            if (graphViewChange.movedElements != null)
            {
                foreach (var element in graphViewChange.movedElements)
                {
                if(element is FluxNodeView nodeView) nodeView.SetPosition(nodeView.GetPosition());
                }
            }

            // Mark the graph asset as dirty to ensure changes are saved.
            EditorUtility.SetDirty(_graph);
            return graphViewChange;
        }

        public void CreateNode(Type nodeType, Vector2 position)
        {
            if (_graph == null) return;
            
            var node = ScriptableObject.CreateInstance(nodeType) as FluxNodeBase;
            node.name = nodeType.Name;
            
            AssetDatabase.AddObjectToAsset(node, _graph);
            EditorUtility.SetDirty(_graph);
            AssetDatabase.SaveAssets();
            
            _graph.AddNode(node);
            var nodeView = CreateNodeView(node);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
        }
        
        /// <summary>
        /// Overridden to build a custom context menu (right-click menu).
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt); // Adds standard options like "Cut", "Copy", "Paste"
            if (_graph == null) return;
            
            // Here, we could add custom actions to the right-click menu.
        }

        public void OnNodeSelectionChanged(FluxNodeView nodeView)
        {
            _window?.OnNodeSelectionChanged(nodeView);
        }

        private FluxNodeView FindNodeView(FluxNodeBase node)
        {
            return GetNodeByGuid(node.NodeId) as FluxNodeView;
        }
    }
}