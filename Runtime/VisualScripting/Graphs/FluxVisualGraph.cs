using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.Attributes;

namespace FluxFramework.VisualScripting.Graphs
{
    /// <summary>
    /// Represents a visual scripting graph containing nodes and connections
    /// </summary>
    [CreateAssetMenu(fileName = "FluxVisualGraph", menuName = "Flux/Visual Scripting/Graph")]
    public class FluxVisualGraph : ScriptableObject
    {
        [SerializeField] private List<FluxNodeBase> _nodes = new List<FluxNodeBase>();
        [SerializeField] private List<FluxNodeConnection> _connections = new List<FluxNodeConnection>();
        [SerializeField] private string _description = "";
        [SerializeField] private bool _autoExecuteOnStart = false;

        /// <summary>
        /// All nodes in this graph
        /// </summary>
        public IReadOnlyList<FluxNodeBase> Nodes => _nodes.AsReadOnly();

        /// <summary>
        /// All connections in this graph
        /// </summary>
        public IReadOnlyList<FluxNodeConnection> Connections => _connections.AsReadOnly();

        /// <summary>
        /// Description of this graph
        /// </summary>
        public string Description 
        { 
            get => _description; 
            set => _description = value; 
        }

        /// <summary>
        /// Whether to automatically execute this graph on start
        /// </summary>
        public bool AutoExecuteOnStart 
        { 
            get => _autoExecuteOnStart; 
            set => _autoExecuteOnStart = value; 
        }

        /// <summary>
        /// Event raised when the graph execution starts
        /// </summary>
        public event Action<FluxVisualGraph> OnExecutionStarted;

        /// <summary>
        /// Event raised when the graph execution completes
        /// </summary>
        public event Action<FluxVisualGraph> OnExecutionCompleted;

        /// <summary>
        /// Event raised when a node is executed
        /// </summary>
        public event Action<FluxNodeBase> OnNodeExecuted;

        /// <summary>
        /// Add a node to the graph
        /// </summary>
        public void AddNode(FluxNodeBase node)
        {
            if (node != null && !_nodes.Contains(node))
            {
                _nodes.Add(node);
                node.OnExecuted += HandleNodeExecuted;
            }
        }

        /// <summary>
        /// Remove a node from the graph
        /// </summary>
        public void RemoveNode(FluxNodeBase node)
        {
            if (node != null && _nodes.Contains(node))
            {
                // Remove all connections involving this node
                _connections.RemoveAll(c => c.FromNode == node || c.ToNode == node);
                
                _nodes.Remove(node);
                node.OnExecuted -= HandleNodeExecuted;
            }
        }

        /// <summary>
        /// Create a connection between two nodes
        /// </summary>
        public bool CreateConnection(FluxNodeBase fromNode, string fromPort, FluxNodeBase toNode, string toPort)
        {
            if (fromNode == null || toNode == null) return false;
            if (!_nodes.Contains(fromNode) || !_nodes.Contains(toNode)) return false;

            var fromPortObj = fromNode.OutputPorts.FirstOrDefault(p => p.DisplayName == fromPort);
            var toPortObj = toNode.InputPorts.FirstOrDefault(p => p.DisplayName == toPort);

            if (fromPortObj == null || toPortObj == null) return false;
            if (!fromPortObj.CanConnectTo(toPortObj)) return false;

            // Remove existing connection to the input port
            _connections.RemoveAll(c => c.ToNode == toNode && c.ToPort == toPort);

            // Create new connection
            var connection = new FluxNodeConnection(fromNode, fromPort, toNode, toPort);
            _connections.Add(connection);

            // Update port connection status
            fromPortObj.IsConnected = true;
            fromPortObj.ConnectedPort = toPortObj;
            toPortObj.IsConnected = true;
            toPortObj.ConnectedPort = fromPortObj;

            return true;
        }

        /// <summary>
        /// Get all connections that provide input to a specific node
        /// </summary>
        public IEnumerable<FluxNodeConnection> GetInputConnections(FluxNodeBase node)
        {
            return _connections.Where(c => c.ToNode == node);
        }

        /// <summary>
        /// Get all connections that come from outputs of a specific node
        /// </summary>
        public IEnumerable<FluxNodeConnection> GetOutputConnections(FluxNodeBase node)
        {
            return _connections.Where(c => c.FromNode == node);
        }

        /// <summary>
        /// Get the connection for a specific input port of a node
        /// </summary>
        public FluxNodeConnection GetInputConnection(FluxNodeBase node, string portName)
        {
            return _connections.FirstOrDefault(c => c.ToNode == node && c.ToPort == portName);
        }

        /// <summary>
        /// Get all connections from a specific output port of a node
        /// </summary>
        public IEnumerable<FluxNodeConnection> GetOutputConnections(FluxNodeBase node, string portName)
        {
            return _connections.Where(c => c.FromNode == node && c.FromPort == portName);
        }

        /// <summary>
        /// Remove a connection
        /// </summary>
        public void RemoveConnection(FluxNodeConnection connection)
        {
            if (_connections.Remove(connection))
            {
                // Update port connection status
                var fromPort = connection.FromNode.OutputPorts.FirstOrDefault(p => p.DisplayName == connection.FromPort);
                var toPort = connection.ToNode.InputPorts.FirstOrDefault(p => p.DisplayName == connection.ToPort);

                if (fromPort != null)
                {
                    fromPort.IsConnected = false;
                    fromPort.ConnectedPort = null;
                }

                if (toPort != null)
                {
                    toPort.IsConnected = false;
                    toPort.ConnectedPort = null;
                }
            }
        }

        /// <summary>
        /// Update all port connection statuses based on current connections
        /// </summary>
        public void UpdateConnectionStatuses()
        {
            // First, reset all ports to disconnected
            foreach (var node in _nodes)
            {
                foreach (var port in node.InputPorts)
                {
                    port.IsConnected = false;
                    port.ConnectedPort = null;
                }
                foreach (var port in node.OutputPorts)
                {
                    port.IsConnected = false;
                    port.ConnectedPort = null;
                }
            }

            // Then, set connected status based on actual connections
            foreach (var connection in _connections)
            {
                var fromPort = connection.FromNode.OutputPorts.FirstOrDefault(p => p.DisplayName == connection.FromPort);
                var toPort = connection.ToNode.InputPorts.FirstOrDefault(p => p.DisplayName == connection.ToPort);

                if (fromPort != null && toPort != null)
                {
                    fromPort.IsConnected = true;
                    fromPort.ConnectedPort = toPort;
                    toPort.IsConnected = true;
                    toPort.ConnectedPort = fromPort;
                }
            }
        }

        /// <summary>
        /// Validate the entire graph
        /// </summary>
        public bool Validate()
        {
            // Update connection status before validation
            UpdateConnectionStatuses();

            bool isValid = true;
            var invalidNodes = new List<string>();

            foreach (var node in _nodes)
            {
                if (!node.Validate())
                {
                    isValid = false;
                    invalidNodes.Add($"{node.NodeName} ({node.GetType().Name})");
                    Debug.LogWarning($"Node validation failed: {node.NodeName} - {node.GetType().Name}");
                }
            }

            if (!isValid)
            {
                Debug.LogError($"Graph validation failed. Invalid nodes: {string.Join(", ", invalidNodes)}");
            }

            return isValid;
        }

        /// <summary>
        /// Update the connection status of all ports based on current connections
        /// </summary>
        private void UpdateConnectionStatus()
        {
            // Reset all connection status
            foreach (var node in _nodes)
            {
                foreach (var port in node.InputPorts)
                {
                    port.IsConnected = false;
                    port.ConnectedPort = null;
                }
                foreach (var port in node.OutputPorts)
                {
                    port.IsConnected = false;
                    port.ConnectedPort = null;
                }
            }

            // Update based on current connections
            foreach (var connection in _connections)
            {
                var fromPort = connection.FromNode.OutputPorts.FirstOrDefault(p => p.Name == connection.FromPort);
                var toPort = connection.ToNode.InputPorts.FirstOrDefault(p => p.Name == connection.ToPort);

                if (fromPort != null && toPort != null)
                {
                    fromPort.IsConnected = true;
                    fromPort.ConnectedPort = toPort;
                    toPort.IsConnected = true;
                    toPort.ConnectedPort = fromPort;
                }
            }
        }

        /// <summary>
        /// Find all nodes of a specific type
        /// </summary>
        public List<T> FindNodes<T>() where T : FluxNodeBase
        {
            return _nodes.OfType<T>().ToList();
        }

        /// <summary>
        /// Find a node by its ID
        /// </summary>
        public FluxNodeBase FindNode(string nodeId)
        {
            return _nodes.FirstOrDefault(n => n.NodeId == nodeId);
        }

        /// <summary>
        /// Get all connections for a specific node
        /// </summary>
        public List<FluxNodeConnection> GetNodeConnections(FluxNodeBase node)
        {
            return _connections.Where(c => c.FromNode == node || c.ToNode == node).ToList();
        }

        /// <summary>
        /// Get output connections for a specific node (alternative implementation)
        /// </summary>
        public List<FluxNodeConnection> GetOutputConnectionsList(FluxNodeBase node)
        {
            return _connections.Where(c => c.FromNode == node).ToList();
        }

        private void HandleNodeExecuted(IFluxNode node)
        {
            OnNodeExecuted?.Invoke(node as FluxNodeBase);
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Opens this graph in the Flux Visual Scripting Editor
        /// </summary>
        [FluxButton("🔧 Open in Editor")]
        private void OpenInEditor()
        {
            // Use reflection to avoid direct assembly reference to Editor
            var editorAssembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.EditorWindow));
            if (editorAssembly != null)
            {
                var windowType = System.Type.GetType("FluxFramework.VisualScripting.Editor.FluxVisualScriptingWindow, FluxFramework.Editor");
                if (windowType != null)
                {
                    // Show the window
                    var showWindowMethod = windowType.GetMethod("ShowWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var window = showWindowMethod?.Invoke(null, null);
                    
                    if (window != null)
                    {
                        // Load this graph in the window
                        var loadGraphMethod = windowType.GetMethod("LoadGraph", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (loadGraphMethod != null)
                        {
                            loadGraphMethod.Invoke(window, new object[] { this });
                        }
                        else
                        {
                            // Fallback: try to set the graph via property
                            var graphProperty = windowType.GetProperty("CurrentGraph", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            graphProperty?.SetValue(window, this);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("FluxVisualScriptingWindow not found. Make sure the Editor assembly is loaded.");
                }
            }
        }

        /// <summary>
        /// Shows graph statistics in the console
        /// </summary>
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
                     $"Description: {(!string.IsNullOrEmpty(_description) ? _description : "No description")}");
        }

        /// <summary>
        /// Validates the graph and shows results in console
        /// </summary>
        [FluxButton("✓ Validate Graph")]
        private void ValidateGraphWithLog()
        {
            bool isValid = Validate();
            
            if (isValid)
            {
                Debug.Log($"✅ Graph '{name}' validation passed!\nNodes: {_nodes.Count}, Connections: {_connections.Count}");
            }
            else
            {
                Debug.LogError($"❌ Graph '{name}' validation failed! Check for missing connections or invalid node configurations.");
            }
        }
        #endif

        private void OnValidate()
        {
            // Clean up null references
            _nodes.RemoveAll(n => n == null);
            _connections.RemoveAll(c => c.FromNode == null || c.ToNode == null);
            
            // Update connection status
            UpdateConnectionStatus();
        }
    }
}
