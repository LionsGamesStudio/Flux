using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// The engine responsible for interpreting and executing a FluxVisualGraph at runtime.
    /// It operates as a state machine, processing a queue of ExecutionTokens. This architecture
    /// is robust, supports concurrent flows (e.g., ForEach), and handles sub-graph execution.
    /// </summary>
    public class FluxGraphExecutor
    {
        private readonly FluxVisualGraph _mainGraph;
        public IGraphRunner Runner { get; }

        private readonly Queue<ExecutionToken> _executionQueue = new Queue<ExecutionToken>();
        private readonly Dictionary<string, object> _nodeOutputCache = new Dictionary<string, object>();

        public FluxGraphExecutor(FluxVisualGraph graph, IGraphRunner runner)
        {
            _mainGraph = graph ?? throw new ArgumentNullException(nameof(graph));
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            
            InitializeNodeGraphReferences(_mainGraph);
        }

        /// <summary>
        /// Recursively sets the ParentGraph property on all AttributedNodeWrappers within a graph and its sub-graphs.
        /// This is crucial for nodes to be able to find their connections at runtime.
        /// </summary>
        private void InitializeNodeGraphReferences(FluxVisualGraph graph)
        {
            if (graph == null) return;
            foreach(var node in graph.Nodes)
            {
                if(node is AttributedNodeWrapper wrapper)
                {
                    wrapper.ParentGraph = graph;
                    if(wrapper.NodeLogic is SubGraphNode subGraphNode)
                    {
                        InitializeNodeGraphReferences(subGraphNode.subGraph);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the graph by creating initial tokens at all entry points and then begins processing the queue.
        /// </summary>
        public void Start()
        {
            _executionQueue.Clear();
            foreach (var node in FindEntryNodes(_mainGraph))
            {
                _executionQueue.Enqueue(new ExecutionToken(node));
            }
            ProcessQueue();
        }

        /// <summary>
        /// The main execution loop. It processes tokens from the queue one by one until it's empty.
        /// </summary>
        private void ProcessQueue()
        {
            while (_executionQueue.Count > 0)
            {
                var token = _executionQueue.Dequeue();
                if (token.TargetNode == null) continue;

                _nodeOutputCache.Clear();
                var newTokens = ExecuteNode(token).ToList(); // .ToList() forces the iterator to execute immediately
                
                foreach (var newToken in newTokens)
                {
                    #if UNITY_EDITOR
                    var connection = FindConnectionBetween(token.TargetNode, newToken.TargetNode);
                    if(connection != null) GraphDebugger.TokenTraverse(connection);
                    #endif
                    _executionQueue.Enqueue(newToken);
                }
            }
        }
        
        /// <summary>
        /// (Public API for Nodes) Adds a new token to the execution queue.
        /// This is the primary mechanism for asynchronous nodes to resume an execution flow.
        /// </summary>
        public void ContinueFlow(ExecutionToken token)
        {
            if (token == null) return;
            _executionQueue.Enqueue(token);
            if (_executionQueue.Count == 1)
            {
                ProcessQueue();
            }
        }
        
        /// <summary>
        /// Executes a single node based on an incoming token and returns an iterator for the subsequent tokens.
        /// This is the core of the execution logic, handling sub-graph transitions and regular node execution.
        /// </summary>
        private IEnumerable<ExecutionToken> ExecuteNode(ExecutionToken token)
        {
            var node = token.TargetNode;
            if (node == null) yield break;

            #if UNITY_EDITOR
            GraphDebugger.NodeEnter(node);
            #endif

            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                var activeGraph = wrapper.ParentGraph;
                var dataInputs = PullDataForNode(node, token, activeGraph);
                string triggeredPortName = token.GetData<string>("_triggeredPortName");

                // --- CASE 1: ENTERING A SUB-GRAPH ---
                if (logic is SubGraphNode subGraphNode && subGraphNode.subGraph != null)
                {
                    var entryPointNode = subGraphNode.subGraph.Nodes.OfType<AttributedNodeWrapper>()
                        .FirstOrDefault(w => w.NodeLogic is GraphInputNode);

                    if (entryPointNode != null)
                    {
                        var subGraphToken = new ExecutionToken(entryPointNode, token);
                        subGraphToken.CallStack.Push(wrapper);
                        subGraphToken.SetData("_triggeredPortName", triggeredPortName);
                        
                        foreach (var (key, value) in dataInputs)
                        {
                            subGraphToken.SetData(key, value);
                        }
                        
                        yield return subGraphToken;
                    }
                    #if UNITY_EDITOR
                    GraphDebugger.NodeExit(node);
                    #endif
                    yield break;
                }

                // --- CASE 2: EXITING A SUB-GRAPH ---
                if (logic is GraphOutputNode)
                {
                    if (token.CallStack.TryPop(out var parentSubGraphNode))
                    {
                        var nextNodeInParent = parentSubGraphNode.GetConnectedNode(triggeredPortName);
                        
                        if (nextNodeInParent != null)
                        {
                            var parentToken = new ExecutionToken(nextNodeInParent, token);
                            foreach (var (key, value) in dataInputs)
                            {
                                parentToken.SetData(key, value);
                            }
                            yield return parentToken;
                        }
                    }
                    #if UNITY_EDITOR
                    GraphDebugger.NodeExit(node);
                    #endif
                    yield break;
                }

                // --- CASE 3: REGULAR NODE EXECUTION ---
                ApplyDataToLogic(logic, node, token, dataInputs);
                
                var executeMethod = logic.GetType().GetMethod("Execute");
                var executionResult = executeMethod?.Invoke(logic, new object[] { this, wrapper, triggeredPortName });

                #if UNITY_EDITOR
                var dataSnapshot = BuildDataSnapshot(logic, node);
                GraphDebugger.UpdateNodeData(node, dataSnapshot);
                GraphDebugger.NodeExit(node);
                #endif

                if (executionResult is IEnumerable<ExecutionToken> resultingTokens)
                {
                    foreach (var resultToken in resultingTokens) yield return resultToken;
                }
                else
                {
                    foreach (var nextToken in FindNextTokensFromPorts(node, activeGraph)) yield return nextToken;
                }
            }
        }

        /// <summary>
        /// Applies data inputs to a node's logic by setting its fields.
        /// It first applies token-local data, then overwrites with connected input port data if available
        /// </summary>
        /// <param name="logic"></param>
        /// <param name="node"></param>
        /// <param name="token"></param>
        /// <param name="dataInputs"></param>
        private void ApplyDataToLogic(INode logic, FluxNodeBase node, ExecutionToken token, Dictionary<string, object> dataInputs)
        {
            var logicType = logic.GetType();

            // 1. Apply token-local data (from ForEach, sub-graph inputs, etc.) to the node's fields.
            foreach (var port in node.OutputPorts.Concat(node.InputPorts).Where(p => p.PortType == FluxPortType.Data))
            {
                var field = logicType.GetField(port.Name);
                if (field != null)
                {
                    object tokenData = token.GetData<object>(field.Name);
                    if (tokenData != null) field.SetValue(logic, tokenData);
                }
            }

            // 2. Apply data from connected input data ports, potentially overwriting token data if connected.
            foreach (var kvp in dataInputs)
            {
                var field = logicType.GetField(kvp.Key);
                field?.SetValue(logic, kvp.Value);
            }
        }

        /// <summary>
        /// Gathers data inputs for a node by traversing its connected input data ports.
        /// It retrieves values from source nodes, using caching to optimize repeated accesses.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="token"></param>
        /// <param name="activeGraph"></param>
        /// <returns></returns>
        private Dictionary<string, object> PullDataForNode(FluxNodeBase node, ExecutionToken token, FluxVisualGraph activeGraph)
        {
            var dataInputs = new Dictionary<string, object>();
            foreach (var inputPort in node.InputPorts.Where(p => p.PortType == FluxPortType.Data))
            {
                var connection = activeGraph.Connections.FirstOrDefault(c => c.ToNodeId == node.NodeId && c.ToPortName == inputPort.Name);
                if (connection != null)
                {
                    var sourceNode = activeGraph.Nodes.FirstOrDefault(n => n.NodeId == connection.FromNodeId);
                    if (sourceNode != null)
                    {
                        object value = GetOutputValue(sourceNode, connection.FromPortName, activeGraph, token);
                        dataInputs[inputPort.Name] = value;
                    }
                }
            }
            return dataInputs;
        }

        /// <summary>
        /// Retrieves the output value from a source node's specified port.
        /// It uses caching to avoid redundant calculations within the same execution step.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="portName"></param>
        /// <param name="currentGraph"></param>
        /// <param name="requestingToken"></param>
        /// <returns></returns>
        private object GetOutputValue(FluxNodeBase node, string portName, FluxVisualGraph currentGraph, ExecutionToken requestingToken)
        {
            string cacheKey = $"{node.NodeId}.{portName}";
            if (_nodeOutputCache.TryGetValue(cacheKey, out object cachedValue))
            {
                return cachedValue;
            }

            // If the source node is the entry point of our current sub-graph, its "output" values
            // are actually the input values stored in our token's local data.
            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is GraphInputNode)
            {
                // If the source node is the entry point of our sub-graph, its "output" values
                // are actually the input values stored in our token's local data.
                var value = requestingToken.GetData<object>(portName);
                _nodeOutputCache[cacheKey] = value;
                return value;
            }

            // For any other data node, we execute it to calculate its values.
            var tempToken = new ExecutionToken(node);
            ExecuteNode(tempToken).ToList();

            _nodeOutputCache.TryGetValue(cacheKey, out var finalValue);
            return finalValue;
        }

        /// <summary>
        /// Finds the next nodes to execute based on the completed node's execution output ports.
        /// If multiple execution paths are available, one is chosen based on port weights.
        /// </summary>
        /// <param name="completedNode"></param>
        /// <param name="activeGraph"></param>
        /// <returns></returns>
        private IEnumerable<ExecutionToken> FindNextTokensFromPorts(FluxNodeBase completedNode, FluxVisualGraph activeGraph)
        {
            var candidates = new List<(FluxNodeConnection connection, FluxNodePort port)>();
            foreach (var outputPort in completedNode.OutputPorts.Where(p => p.PortType == FluxPortType.Execution))
            {
                var connections = activeGraph.Connections.Where(c => c.FromNodeId == completedNode.NodeId && c.FromPortName == outputPort.Name);
                foreach (var connection in connections)
                {
                    candidates.Add((connection, outputPort));
                }
            }

            if (candidates.Count == 0) yield break;

            if (candidates.Count == 1)
            {
                var nextNode = activeGraph.Nodes.FirstOrDefault(n => n.NodeId == candidates[0].connection.ToNodeId);
                if (nextNode != null)
                {
                    var newToken = new ExecutionToken(nextNode);
                    newToken.SetData("_triggeredPortName", candidates[0].connection.ToPortName);
                    yield return newToken;
                }
                yield break;
            }

            var chosenConnection = ChooseWeightedRandomConnection(candidates);
            var chosenNode = activeGraph.Nodes.FirstOrDefault(n => n.NodeId == chosenConnection.ToNodeId);
            if (chosenNode != null)
            {
                var newToken = new ExecutionToken(chosenNode);
                newToken.SetData("_triggeredPortName", chosenConnection.ToPortName);
                yield return newToken;
            }
        }
        
        /// <summary>
        /// Selects one connection from a list of candidates based on their probability weights.
        /// If all weights are zero or negative, it defaults to the first connection.
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        private FluxNodeConnection ChooseWeightedRandomConnection(List<(FluxNodeConnection connection, FluxNodePort port)> candidates)
        {
            float totalWeight = candidates.Sum(c => Mathf.Max(0, c.port.ProbabilityWeight));
            if (totalWeight <= 0) return candidates.First().connection;
            float randomPoint = UnityEngine.Random.Range(0, totalWeight);
            foreach (var candidate in candidates)
            {
                float weight = Mathf.Max(0, candidate.port.ProbabilityWeight);
                if (randomPoint < weight) return candidate.connection;
                randomPoint -= weight;
            }
            return candidates.Last().connection;
        }

        /// <summary>
        /// Finds all entry nodes in the graph that have no incoming execution connections.
        /// These nodes will be the starting points for execution when the graph begins.
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        private IEnumerable<FluxNodeBase> FindEntryNodes(FluxVisualGraph graph)
        {
            var connectedInputPorts = graph.Connections
                .Select(c => (c.ToNodeId, c.ToPortName))
                .ToHashSet();
            foreach (var node in graph.Nodes)
            {
                if (!node.OutputPorts.Any(p => p.PortType == FluxPortType.Execution)) continue;
                var executionInputs = node.InputPorts.Where(p => p.PortType == FluxPortType.Execution).ToList();
                if (!executionInputs.Any())
                {
                    yield return node;
                    continue;
                }
                if (!executionInputs.Any(p => connectedInputPorts.Contains((node.NodeId, p.Name))))
                {
                    yield return node;
                }
            }
        }
        
        /// <summary>
        /// Finds the connection object between two nodes, if it exists.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private FluxNodeConnection FindConnectionBetween(FluxNodeBase from, FluxNodeBase to)
        {
            if (from == null || to == null) return null;
            var fromGraph = (from as AttributedNodeWrapper)?.ParentGraph;
            if (fromGraph == null) return null;
            return fromGraph.Connections.FirstOrDefault(c => c.FromNodeId == from.NodeId && c.ToNodeId == to.NodeId);
        }

        #if UNITY_EDITOR
        private Dictionary<string, string> BuildDataSnapshot(INode logic, FluxNodeBase node)
        {
            var dataSnapshot = new Dictionary<string, string>();
            var logicType = logic.GetType();
            foreach (var port in node.InputPorts.Concat(node.OutputPorts).Where(p => p.PortType == FluxPortType.Data))
            {
                var field = logicType.GetField(port.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    object value = field.GetValue(logic);
                    dataSnapshot[port.Name] = (value == null) ? "null" : value.ToString();
                }
            }
            return dataSnapshot;
        }
        #endif
    }
}