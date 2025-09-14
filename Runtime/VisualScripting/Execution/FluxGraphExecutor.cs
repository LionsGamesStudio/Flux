using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// The engine responsible for interpreting and executing a FluxVisualGraph at runtime.
    /// An instance of this class represents a single, stateful execution of a graph.
    /// </summary>
    public class FluxGraphExecutor
    {
        private readonly FluxVisualGraph _graph;
        public IGraphRunner Runner { get; }

        // Caches the calculated output values of data nodes during a single execution tick.
        private readonly Dictionary<string, object> _nodeOutputCache = new Dictionary<string, object>();

        public FluxGraphExecutor(FluxVisualGraph graph, IGraphRunner runner)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>
        /// Begins execution of the graph from all its designated entry points (e.g., Start nodes).
        /// </summary>
        public void Start()
        {
            var entryNodes = FindEntryNodes();
            foreach (var node in entryNodes)
            {
                ExecuteFlowFrom(node);
            }
        }

        /// <summary>
        /// Executes a single flow starting from a specific node.
        /// </summary>
        private void ExecuteFlowFrom(FluxNodeBase node)
        {
            if (node == null) return;
            
            _nodeOutputCache.Clear(); // Clear cache for each new execution flow.

            // 1. Execute the node itself
            var nodeOutputs = ExecuteNode(node);
            
            // 2. Find the next node in the execution chain
            var executionOutputPort = node.OutputPorts.FirstOrDefault(p => p.PortType == FluxPortType.Execution);
            if (executionOutputPort != null)
            {
                var connection = _graph.Connections.FirstOrDefault(c => c.FromNodeId == node.NodeId && c.FromPortName == executionOutputPort.Name);
                if (connection != null)
                {
                    var nextNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == connection.ToNodeId);
                    ExecuteFlowFrom(nextNode);
                }
            }
        }
        
        /// <summary>
        /// Executes a single node, calculating its data inputs first.
        /// </summary>
        private Dictionary<string, object> ExecuteNode(FluxNodeBase node)
        {
            var dataInputs = new Dictionary<string, object>();

            // --- PULL DATA ---
            // Before executing the node, we calculate the value for each of its data input ports.
            foreach (var inputPort in node.InputPorts.Where(p => p.PortType == FluxPortType.Data))
            {
                var connection = _graph.Connections.FirstOrDefault(c => c.ToNodeId == node.NodeId && c.ToPortName == inputPort.Name);
                if (connection != null)
                {
                    var sourceNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == connection.FromNodeId);
                    if (sourceNode != null)
                    {
                        // Recursively get the value from the source node.
                        object value = GetOutputValue(sourceNode, connection.FromPortName);
                        dataInputs[inputPort.Name] = value;
                    }
                }
            }
            
            // --- EXECUTE LOGIC ---
            // For now, we only support AttributedNodeWrapper.
            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                var logicType = logic.GetType();
                
                // 1. Apply the calculated data inputs to the logic's fields.
                foreach (var kvp in dataInputs)
                {
                    var field = logicType.GetField(kvp.Key);
                    field?.SetValue(logic, kvp.Value);
                }
                
                // 2. Call the 'Execute' method.
                var executeMethod = logicType.GetMethod("Execute");
                executeMethod?.Invoke(logic, new object[] { this, wrapper, null }); // Pass context, no specific port name for now.
                
                // 3. Extract the output data from the logic's fields.
                var dataOutputs = new Dictionary<string, object>();
                foreach (var outputPort in node.OutputPorts.Where(p => p.PortType == FluxPortType.Data))
                {
                    var field = logicType.GetField(outputPort.Name);
                    if (field != null)
                    {
                        dataOutputs[outputPort.Name] = field.GetValue(logic);
                    }
                }
                return dataOutputs;
            }
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Recursively gets an output value from a node, using a cache to avoid re-calculation.
        /// </summary>
        private object GetOutputValue(FluxNodeBase node, string portName)
        {
            string cacheKey = $"{node.NodeId}.{portName}";
            if (_nodeOutputCache.TryGetValue(cacheKey, out object cachedValue))
            {
                return cachedValue;
            }

            // If not in cache, execute the node to calculate its outputs.
            var allOutputs = ExecuteNode(node);
            
            // Cache all outputs from this execution.
            foreach (var kvp in allOutputs)
            {
                _nodeOutputCache[$"{node.NodeId}.{kvp.Key}"] = kvp.Value;
            }

            return allOutputs.GetValueOrDefault(portName);
        }

        /// <summary>
        /// Finds all nodes that should start an execution flow.
        /// For now, these are nodes that have an execution output but no execution input.
        /// </summary>
        private IEnumerable<FluxNodeBase> FindEntryNodes()
        {
            return _graph.Nodes.Where(n => 
                n.OutputPorts.Any(p => p.PortType == FluxPortType.Execution) && 
                !n.InputPorts.Any(p => p.PortType == FluxPortType.Execution)
            );
        }
    }
}