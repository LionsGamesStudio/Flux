using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluxFramework.VisualScripting.Graphs
{
    /// <summary>
    /// Represents a visual scripting graph asset containing nodes and connections.
    /// This is the central data model for a visual script.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxVisualGraph", menuName = "Flux/Visual Scripting/Graph")]
    public class FluxVisualGraph : ScriptableObject
    {
        [SerializeField] private List<FluxNodeBase> _nodes = new List<FluxNodeBase>();
        [SerializeField] private List<FluxNodeConnection> _connections = new List<FluxNodeConnection>();
        [SerializeField] private string _description = "";
        [SerializeField] private bool _autoExecuteOnStart = false;

        /// <summary> A read-only list of all nodes in this graph. </summary>
        public IReadOnlyList<FluxNodeBase> Nodes => _nodes.AsReadOnly();
        /// <summary> A read-only list of all connections in this graph. </summary>
        public IReadOnlyList<FluxNodeConnection> Connections => _connections.AsReadOnly();
        /// <summary> A user-defined description of what this graph does. </summary>
        public string Description { get => _description; set => _description = value; }
        /// <summary> If true, a runner will automatically execute this graph on Start. </summary>
        public bool AutoExecuteOnStart { get => _autoExecuteOnStart; set => _autoExecuteOnStart = value; }

        /// <summary> Event fired when the graph execution starts. </summary>
        public event Action<FluxVisualGraph> OnExecutionStarted;
        /// <summary> Event fired when the graph execution completes. </summary>
        public event Action<FluxVisualGraph> OnExecutionCompleted;
        /// <summary> Event fired when a node is executed. </summary>
        public event Action<FluxNodeBase> OnNodeExecuted;

        /// <summary> Adds a node to the graph's internal list. </summary>
        public void AddNode(FluxNodeBase node)
        {
            if (node != null && !_nodes.Contains(node))
            {
                _nodes.Add(node);
                node.OnExecuted += HandleNodeExecuted;
            }
        }

        /// <summary> Removes a node and all of its connections from the graph. </summary>
        public void RemoveNode(FluxNodeBase node)
        {
            if (node != null && _nodes.Contains(node))
            {
                _connections.RemoveAll(c => c.FromNode == node || c.ToNode == node);
                _nodes.Remove(node);
                node.OnExecuted -= HandleNodeExecuted;
                
                #if UNITY_EDITOR
                if (AssetDatabase.IsSubAsset(node))
                {
                    AssetDatabase.RemoveObjectFromAsset(node);
                }
                #endif
            }
        }

        /// <summary> Sets the editor position of a node. Called by the GraphView. </summary>
        public void SetNodePosition(FluxNodeBase node, Vector2 newPosition)
        {
            if (node != null && _nodes.Contains(node))
            {
                node.Position = newPosition;
            }
        }

        /// <summary> Duplicates a node, adds it to this graph asset, and registers it. Used by the paste logic. </summary>
        public FluxNodeBase DuplicateNode(FluxNodeBase original)
        {
            if (original == null) return null;
            var clone = original.Clone();
            
            #if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(clone, this);
            #endif

            AddNode(clone);
            return clone;
        }

        /// <summary> Creates a connection between two nodes using their internal port names. </summary>
        public bool CreateConnection(FluxNodeBase fromNode, string fromPortName, FluxNodeBase toNode, string toPortName)
        {
            if (fromNode == null || toNode == null) return false;

            var fromPortObj = fromNode.OutputPorts.FirstOrDefault(p => p.Name == fromPortName);
            var toPortObj = toNode.InputPorts.FirstOrDefault(p => p.Name == toPortName);

            if (fromPortObj == null || toPortObj == null || !fromPortObj.CanConnectTo(toPortObj)) return false;

            _connections.RemoveAll(c => c.ToNode == toNode && c.ToPort == toPortName);

            var connection = new FluxNodeConnection(fromNode, fromPortName, toNode, toPortName);
            _connections.Add(connection);
            return true;
        }
        
        /// <summary> Gets all connections that provide input to a specific node. </summary>
        public IEnumerable<FluxNodeConnection> GetInputConnections(FluxNodeBase node) => _connections.Where(c => c.ToNode == node);
        /// <summary> Gets all connections that come from outputs of a specific node. </summary>
        public IEnumerable<FluxNodeConnection> GetOutputConnections(FluxNodeBase node) => _connections.Where(c => c.FromNode == node);
        /// <summary> Gets the connection for a specific input port of a node. </summary>
        public FluxNodeConnection GetInputConnection(FluxNodeBase node, string portName) => _connections.FirstOrDefault(c => c.ToNode == node && c.ToPort == portName);
        /// <summary> Gets all connections from a specific output port of a node. </summary>
        public IEnumerable<FluxNodeConnection> GetOutputConnections(FluxNodeBase node, string portName) => _connections.Where(c => c.FromNode == node && c.FromPort == portName);

        /// <summary> Removes a specific connection instance from the graph. </summary>
        public void RemoveConnection(FluxNodeConnection connection)
        {
            if (connection != null)
            {
                _connections.Remove(connection);
            }
        }
        
        /// <summary> Overload to remove a connection by specifying its source and target nodes and internal port names. </summary>
        public void RemoveConnection(FluxNodeBase fromNode, string fromPortName, FluxNodeBase toNode, string toPortName)
        {
            var connectionToRemove = _connections.FirstOrDefault(c =>
                c.FromNode == fromNode && c.FromPort == fromPortName &&
                c.ToNode == toNode && c.ToPort == toPortName);
            
            RemoveConnection(connectionToRemove);
        }

        /// <summary> Updates all port connection statuses based on current connections. </summary>
        public void UpdateConnectionStatuses()
        {
            var allPorts = _nodes.SelectMany(n => n.InputPorts.Concat(n.OutputPorts));
            foreach (var port in allPorts)
            {
                port.IsConnected = false;
                port.ConnectedPort = null;
            }
            foreach (var connection in _connections)
            {
                var fromPort = connection.FromNode?.OutputPorts.FirstOrDefault(p => p.Name == connection.FromPort);
                var toPort = connection.ToNode?.InputPorts.FirstOrDefault(p => p.Name == connection.ToPort);

                if (fromPort != null && toPort != null)
                {
                    fromPort.IsConnected = true; fromPort.ConnectedPort = toPort;
                    toPort.IsConnected = true; toPort.ConnectedPort = fromPort;
                }
            }
        }

        /// <summary> Validates the entire graph, checking for unconnected required ports. </summary>
        public bool Validate()
        {
            UpdateConnectionStatuses();
            bool isValid = true;
            foreach (var node in _nodes)
            {
                if (!node.Validate())
                {
                    isValid = false;
                    Debug.LogWarning($"Node validation failed: '{node.NodeName}' requires one or more of its input ports to be connected.", node);
                }
            }
            if (!isValid)
            {
                Debug.LogError($"Graph validation failed for '{this.name}'. Check warnings for details.", this);
            }
            return isValid;
        }
        
        /// <summary> Finds all nodes of a specific type in the graph. </summary>
        public List<T> FindNodes<T>() where T : FluxNodeBase => _nodes.OfType<T>().ToList();

        /// <summary> Finds a node by its unique ID. </summary>
        public FluxNodeBase FindNode(string nodeId) => _nodes.FirstOrDefault(n => n.NodeId == nodeId);

        /// <summary> Gets all connections (input and output) for a specific node. </summary>
        public List<FluxNodeConnection> GetNodeConnections(FluxNodeBase node) => _connections.Where(c => c.FromNode == node || c.ToNode == node).ToList();
        
        /// <summary> Gets all output connections for a specific node (alternative implementation). </summary>
        public List<FluxNodeConnection> GetOutputConnectionsList(FluxNodeBase node) => _connections.Where(c => c.FromNode == node).ToList();

        /// <summary> Internal handler to bubble up the OnNodeExecuted event. </summary>
        private void HandleNodeExecuted(IFluxNode node)
        {
            OnNodeExecuted?.Invoke(node as FluxNodeBase);
        }

        #if UNITY_EDITOR
        [FluxButton("📈 Show Statistics")]
        private void ShowStatistics()
        {
            var nodeCount = _nodes.Count;
            var connectionCount = _connections.Count;
            var nodeTypes = _nodes.GroupBy(n => n.GetType().Name).Select(g => $"{g.Key}: {g.Count()}");
            
            Debug.Log($"=== Graph Statistics: {name} ===\n" +
                    $"Nodes: {nodeCount}\n" +
                    $"Connections: {connectionCount}\n" +
                    $"Node Types:\n{string.Join("\n", nodeTypes)}\n" +
                    $"Description: {(!string.IsNullOrEmpty(_description) ? _description : "No description")}", this);
        }
        #endif

        /// <summary> Unity's OnValidate, used to clean up any null references in lists if they occur. </summary>
        private void OnValidate()
        {
            _nodes.RemoveAll(n => n == null);
            _connections.RemoveAll(c => c.FromNode == null || c.ToNode == null);
            UpdateConnectionStatuses();
        }
    }
}