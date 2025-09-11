using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// The engine that executes a visual scripting graph. It manages the flow of execution,
    /// resolves data dependencies, and caches node outputs for a single run. An instance of this class
    /// represents one execution instance of a graph.
    /// </summary>
    public class FluxGraphExecutor
    {
        private readonly FluxVisualGraph _graph;
        private readonly Dictionary<string, object> _nodeOutputCache = new Dictionary<string, object>();
        private readonly HashSet<FluxNodeBase> _executedNodes = new HashSet<FluxNodeBase>();

        /// <summary>
        /// The runner (e.g., a MonoBehaviour) that provides the context for this execution.
        /// Nodes can access this to interact with the scene (e.g., GetContextObject).
        /// </summary>
        public IGraphRunner Runner { get; }

        /// <summary>
        /// Creates a new executor for a specific graph and runner context.
        /// </summary>
        /// <param name="graph">The graph asset to execute.</param>
        /// <param name="runner">The context that is running this graph.</param>
        public FluxGraphExecutor(FluxVisualGraph graph, IGraphRunner runner)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>
        /// Begins execution of the graph from its default entry points (e.g., Start nodes).
        /// </summary>
        public void Execute()
        {
            _nodeOutputCache.Clear();
            _executedNodes.Clear();

            var entryNodes = FindEntryNodes();
            
            if (entryNodes.Count == 0)
            {
                // If there are no execution-based entry points, run all pure data nodes to pre-calculate values.
                ExecuteDataNodes();
            }
            else
            {
                // Start the flow from the identified execution entry points.
                foreach (var entryNode in entryNodes)
                {
                    ExecuteNodeFlow(entryNode);
                }
            }
        }

        /// <summary>
        /// Executes a new, separate execution flow starting from a specified node.
        /// This is used to implement functions or sub-routines within a graph.
        /// </summary>
        public void ExecuteSubFlow(FluxNodeBase startNode)
        {
            if (startNode == null) return;
            // Note: We do NOT clear the executed nodes or cache here, as it's a sub-flow
            // that might need to access data from the main flow.
            ExecuteNodeFlow(startNode);
        }

        /// <summary>
        /// Continues an existing execution flow from a specific output port of a node.
        /// This is the callback mechanism for asynchronous nodes like event listeners or timers.
        /// </summary>
        public void ContinueFromPort(IFluxNode node, string portName, Dictionary<string, object> initialOutputs)
        {
            var startNode = node as FluxNodeBase;
            if (startNode == null) return;

            // Cache the outputs provided by the event/callback so connected nodes can use them.
            if (initialOutputs != null)
            {
                CacheOutputValues(startNode, initialOutputs);
            }
            
            // Find all execution connections from the specified port and continue the flow.
            var executionConnections = _graph.GetOutputConnections(startNode)
                .Where(c => c.FromPort == portName && 
                            startNode.OutputPorts.Any(p => p.Name == c.FromPort && p.PortType == FluxPortType.Execution));

            foreach (var connection in executionConnections)
            {
                ExecuteNodeFlow(connection.ToNode);
            }
        }

        /// <summary>
        /// Executes a single node for debugging purposes.
        /// This does not trigger any subsequent execution flow.
        /// </summary>
        public Dictionary<string, object> ExecuteSingleNodeForDebug(FluxNodeBase node)
        {
            if (node == null || !node.CanExecute) return new Dictionary<string, object>();

            try
            {
                var inputs = GatherInputValues(node);
                node.Execute(this, inputs, out var outputs);
                // We do NOT cache or mark as executed in a debug run.
                return outputs;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during debug execution of node {node.NodeName}: {ex.Message}\n{ex.StackTrace}", node);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Finds nodes that can serve as starting points for an execution flow.
        /// These are typically nodes with an execution input that is not connected to anything.
        /// </summary>
        private List<FluxNodeBase> FindEntryNodes()
        {
            var entryNodes = new List<FluxNodeBase>();
            foreach (var node in _graph.Nodes)
            {
                var executionInputs = node.InputPorts.Where(p => p.PortType == FluxPortType.Execution);
                if (executionInputs.Any())
                {
                    // An entry node is one whose required execution input is not connected.
                    bool isConnected = _graph.GetInputConnections(node).Any(c => c.ToPort == executionInputs.First().Name);
                    if (!isConnected)
                    {
                        entryNodes.Add(node);
                    }
                }
            }
            return entryNodes;
        }
        
        /// <summary>
        /// Executes all pure data nodes in the graph in the correct dependency order.
        /// </summary>
        private void ExecuteDataNodes()
        {
            var dataNodes = _graph.Nodes.Where(n => !n.InputPorts.Any(p => p.PortType == FluxPortType.Execution) && 
                                                    !n.OutputPorts.Any(p => p.PortType == FluxPortType.Execution)).ToList();
            var sortedNodes = TopologicalSort(dataNodes);
            foreach (var node in sortedNodes)
            {
                ExecuteNode(node);
            }
        }

        /// <summary>
        /// Recursively executes a node and all subsequent nodes connected via execution ports.
        /// </summary>
        private void ExecuteNodeFlow(FluxNodeBase node)
        {
            if (node == null || _executedNodes.Contains(node))
                return;

            ExecuteNode(node);

            var executionOutputs = _graph.GetOutputConnections(node)
                .Where(c => node.OutputPorts.Any(p => p.Name == c.FromPort && p.PortType == FluxPortType.Execution));

            foreach (var connection in executionOutputs)
            {
                ExecuteNodeFlow(connection.ToNode);
            }
        }

        /// <summary>
        /// Executes a single node by gathering its inputs and calling its Execute method.
        /// </summary>
        private void ExecuteNode(FluxNodeBase node)
        {
            if (_executedNodes.Contains(node) || !node.CanExecute)
                return;

            try
            {
                var inputs = GatherInputValues(node);
                // The executor passes a reference to itself ('this') to the node, providing the execution context.
                node.Execute(this, inputs, out var outputs);
                CacheOutputValues(node, outputs);
                _executedNodes.Add(node);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing node {node.NodeName}: {ex.Message}\n{ex.StackTrace}", node);
            }
        }

        /// <summary>
        /// Gathers all necessary data inputs for a node by looking at its connections
        /// and retrieving values from the output cache of previously executed nodes.
        /// </summary>
        private Dictionary<string, object> GatherInputValues(FluxNodeBase node)
        {
            var inputs = new Dictionary<string, object>();
            foreach (var inputPort in node.InputPorts)
            {
                if (inputPort.PortType == FluxPortType.Execution) continue;

                var connection = _graph.GetInputConnections(node).FirstOrDefault(c => c.ToPort == inputPort.Name);
                if (connection != null)
                {
                    // A unique key for the output port of the source node.
                    string cacheKey = $"{connection.FromNode.NodeId}.{connection.FromPort}";
                    if (_nodeOutputCache.TryGetValue(cacheKey, out object value))
                    {
                        inputs[inputPort.Name] = value;
                    }
                    else
                    {
                        // If the source node hasn't been executed yet (e.g., a pure data node), execute it now.
                        if (!_executedNodes.Contains(connection.FromNode))
                        {
                            ExecuteNode(connection.FromNode);
                        }
                        // Try getting the value from the cache again after execution.
                        if (_nodeOutputCache.TryGetValue(cacheKey, out value))
                        {
                            inputs[inputPort.Name] = value;
                        }
                    }
                }
            }
            return inputs;
        }

        /// <summary>
        /// Stores the output values of an executed node in a cache for later use by other nodes.
        /// </summary>
        private void CacheOutputValues(FluxNodeBase node, Dictionary<string, object> outputs)
        {
            foreach (var kvp in outputs)
            {
                // Execution ports are signals, not data, so they are not cached.
                if (node.OutputPorts.Any(p => p.Name == kvp.Key && p.PortType == FluxPortType.Execution)) continue;
                
                string cacheKey = $"{node.NodeId}.{kvp.Key}";
                _nodeOutputCache[cacheKey] = kvp.Value;
            }
        }
        
        /// <summary>
        /// Performs a topological sort on a list of nodes to determine a safe execution order
        /// where dependencies are always executed before the nodes that depend on them.
        /// </summary>
        private List<FluxNodeBase> TopologicalSort(List<FluxNodeBase> nodes)
        {
            var sorted = new List<FluxNodeBase>();
            var visited = new HashSet<FluxNodeBase>();
            foreach (var node in nodes)
            {
                if (!visited.Contains(node))
                {
                    TopologicalSortVisit(node, nodes, visited, new HashSet<FluxNodeBase>(), sorted);
                }
            }
            return sorted;
        }

        private void TopologicalSortVisit(FluxNodeBase node, List<FluxNodeBase> allNodes, 
                                          HashSet<FluxNodeBase> visited, HashSet<FluxNodeBase> visiting, 
                                          List<FluxNodeBase> sorted)
        {
            if (visiting.Contains(node))
            {
                Debug.LogWarning($"Circular dependency detected in data graph involving node {node.NodeName}", _graph);
                return;
            }
            if (visited.Contains(node)) return;

            visiting.Add(node);

            var dependencies = _graph.GetInputConnections(node)
                .Where(c => allNodes.Contains(c.FromNode))
                .Select(c => c.FromNode);

            foreach (var dependency in dependencies)
            {
                TopologicalSortVisit(dependency, allNodes, visited, visiting, sorted);
            }

            visiting.Remove(node);
            visited.Add(node);
            sorted.Add(node);
        }
    }
}