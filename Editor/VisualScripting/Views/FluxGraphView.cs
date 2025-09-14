using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxGraphView : GraphView
    {
        private FluxVisualGraph _graph;

        public FluxGraphView()
        {
            // Add background grid
            Insert(0, new GridBackground());

            // Add manipulators for basic interactions
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // This is required to draw the lines for new connections
            var styleSheet = new StyleSheet();
            styleSheet.ImportStyleSheet("Packages/com.unity.visualscripting/Editor/Application/Graphs/Styles/GraphView.uss");
            styleSheets.Add(styleSheet);
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
                    
                    if(fromPortView != null && toPortView != null)
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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create Node/Add", (a) => CreateNode(typeof(AttributedNodeWrapper), a.eventInfo.localMousePosition));
            // We will replace this with a real search window later.
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

        private void CreateNodeView(FluxNodeBase nodeData)
        {
            var nodeView = new FluxNodeView(nodeData);
            AddElement(nodeView);
        }
    }
}