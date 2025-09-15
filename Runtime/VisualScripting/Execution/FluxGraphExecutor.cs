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
    /// It operates as a state machine, processing a queue of ExecutionTokens. This non-recursive
    /// approach is robust and supports concurrent and asynchronous execution flows.
    /// </summary>
    public class FluxGraphExecutor
    {
        private readonly FluxVisualGraph _graph;
        public IGraphRunner Runner { get; }

        private readonly Queue<ExecutionToken> _executionQueue = new Queue<ExecutionToken>();
        private readonly Dictionary<string, object> _nodeOutputCache = new Dictionary<string, object>();
        
        public FluxGraphExecutor(FluxVisualGraph graph, IGraphRunner runner)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));

            foreach(var node in _graph.Nodes)
            {
                if(node is AttributedNodeWrapper wrapper)
                {
                    wrapper.ParentGraph = _graph;
                }
            }
        }

        /// <summary>
        /// Starts the execution of the graph from its entry nodes.
        /// This method initializes the execution queue and processes it until completion.
        /// </summary>
        public void Start()
        {
            _executionQueue.Clear();
            foreach (var node in FindEntryNodes())
            {
                _executionQueue.Enqueue(new ExecutionToken(node));
            }
            ProcessQueue();
        }

        /// <summary>
        /// Processes the execution queue until it is empty.
        /// Each token is dequeued, its target node is executed, and any resulting tokens
        /// are enqueued for further processing.
        /// </summary>
        private void ProcessQueue()
        {
            while (_executionQueue.Count > 0)
            {
                var token = _executionQueue.Dequeue();
                if (token.TargetNode == null) continue;

                _nodeOutputCache.Clear();

                var newTokens = ExecuteNode(token).ToList(); // Use .ToList() to execute the iterator now

                foreach (var newToken in newTokens)
                {
#if UNITY_EDITOR
                    var connection = FindConnectionBetween(token.TargetNode, newToken.TargetNode);
                    if(connection != null)
                    {
                        GraphDebugger.TokenTraverse(connection);
                    }
#endif

                    _executionQueue.Enqueue(newToken);
                }
            }
        }
        
        /// <summary>
        /// Continues the execution flow by enqueuing a new token.
        /// This allows for external triggers to inject new execution paths into the graph.
        /// </summary>
        /// <param name="token"></param>
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
        /// Executes a single node based on the provided execution token.
        /// It gathers input data, invokes the node's logic, and yields any resulting tokens.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private IEnumerable<ExecutionToken> ExecuteNode(ExecutionToken token)
        {
            var node = token.TargetNode;
            if (node == null) yield break;

#if UNITY_EDITOR
            GraphDebugger.NodeEnter(node);
#endif

            var dataInputs = new Dictionary<string, object>();

            foreach (var inputPort in node.InputPorts.Where(p => p.PortType == FluxPortType.Data))
            {
                var connection = _graph.Connections.FirstOrDefault(c => c.ToNodeId == node.NodeId && c.ToPortName == inputPort.Name);
                if (connection != null)
                {
                    var sourceNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == connection.FromNodeId);
                    if (sourceNode != null)
                    {
                        object value = GetOutputValue(sourceNode, connection.FromPortName);
                        dataInputs[inputPort.Name] = value;
                    }
                }
            }

            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                var logicType = logic.GetType();

                foreach (var outputPort in node.OutputPorts.Where(p => p.PortType == FluxPortType.Data))
                {
                    var field = logicType.GetField(outputPort.Name);
                    if (field != null)
                    {
                        object tokenData = token.GetData<object>(field.Name);
                        if (tokenData != null)
                        {
                            field.SetValue(logic, tokenData);
                        }
                    }
                }

                foreach (var kvp in dataInputs)
                {
                    var field = logicType.GetField(kvp.Key);
                    field?.SetValue(logic, kvp.Value);
                }

                var executeMethod = logicType.GetMethod("Execute");
                var executionResult = executeMethod?.Invoke(logic, new object[] { this, wrapper, token });

                #if UNITY_EDITOR
                // After execution, gather all data port values into a dictionary.
                var dataSnapshot = new Dictionary<string, string>();

                // We gather both inputs and outputs to see the full state.
                foreach (var port in node.InputPorts.Concat(node.OutputPorts).Where(p => p.PortType == FluxPortType.Data))
                {
                    var field = logicType.GetField(port.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        object value = field.GetValue(logic);
                        dataSnapshot[port.Name] = (value == null) ? "null" : value.ToString();
                    }
                }
                // Send the data snapshot to the debugger.
                GraphDebugger.UpdateNodeData(node, dataSnapshot);
                #endif

#if UNITY_EDITOR
                GraphDebugger.NodeExit(node);
#endif

                if (executionResult is IEnumerable<ExecutionToken> resultingTokens)
                {
                    foreach (var resultToken in resultingTokens)
                    {
                        yield return resultToken;
                    }
                }
                else
                {
                    foreach (var nextToken in FindNextTokensFromPorts(node))
                    {
                        yield return nextToken;
                    }
                }
            }
            else
            {
#if UNITY_EDITOR
                GraphDebugger.NodeExit(node);
#endif
                yield break;
            }
        }

        /// <summary>
        /// Retrieves the output value from a node's specified port.
        /// If the value is not cached, it executes the node to obtain the value.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="portName"></param>
        /// <returns></returns>
        private object GetOutputValue(FluxNodeBase node, string portName)
        {
            string cacheKey = $"{node.NodeId}.{portName}";
            if (_nodeOutputCache.TryGetValue(cacheKey, out object cachedValue))
            {
                return cachedValue;
            }

            var tempToken = new ExecutionToken(node);
            ExecuteNode(tempToken).ToList();

            var allOutputs = new Dictionary<string, object>();
            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                foreach (var outputPort in node.OutputPorts.Where(p => p.PortType == FluxPortType.Data))
                {
                    var field = logic.GetType().GetField(outputPort.Name);
                    if (field != null)
                    {
                        var value = field.GetValue(logic);
                        _nodeOutputCache[$"{node.NodeId}.{outputPort.Name}"] = value;
                        allOutputs[outputPort.Name] = value;
                    }
                }
            }
            allOutputs.TryGetValue(portName, out var finalValue);
            return finalValue;
        }

        /// <summary>
        /// Finds the next execution tokens based on the completed node's output execution ports.
        /// If multiple outgoing connections exist, one is chosen based on weighted randomness.
        /// </summary>
        /// <param name="completedNode"></param>
        /// <returns></returns>
        private IEnumerable<ExecutionToken> FindNextTokensFromPorts(FluxNodeBase completedNode)
        {
            var candidates = new List<(FluxNodeConnection connection, FluxNodePort port)>();
            foreach (var outputPort in completedNode.OutputPorts.Where(p => p.PortType == FluxPortType.Execution))
            {
                var connections = _graph.Connections.Where(c => c.FromNodeId == completedNode.NodeId && c.FromPortName == outputPort.Name);
                foreach (var connection in connections)
                {
                    candidates.Add((connection, outputPort));
                }
            }

            if (candidates.Count == 0)
            {
                yield break;
            }

            if (candidates.Count == 1)
            {
                var nextNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == candidates[0].connection.ToNodeId);
                if (nextNode != null)
                {
                    yield return new ExecutionToken(nextNode);
                }
                yield break;
            }

            var chosenConnection = ChooseWeightedRandomConnection(candidates);
            var chosenNode = _graph.Nodes.FirstOrDefault(n => n.NodeId == chosenConnection.ToNodeId);
            if (chosenNode != null)
            {
                yield return new ExecutionToken(chosenNode);
            }
        }
        
        /// <summary>
        /// Selects one connection from a list of candidates based on their associated port weights.
        /// If all weights are zero or negative, the first connection is returned.
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
        /// Finds all entry nodes in the graph.
        /// Entry nodes are defined as nodes with execution output ports that have no incoming execution connections.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<FluxNodeBase> FindEntryNodes()
        {
            var connectedInputPorts = _graph.Connections
                .Select(c => (c.ToNodeId, c.ToPortName))
                .ToHashSet();
            foreach (var node in _graph.Nodes)
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
        /// Finds the connection between two nodes, if it exists.
        /// This is used for visual debugging to highlight the path of execution.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private FluxNodeConnection FindConnectionBetween(FluxNodeBase from, FluxNodeBase to)
        {
            if (from == null || to == null) return null;
            return _graph.Connections.FirstOrDefault(c => c.FromNodeId == from.NodeId && c.ToNodeId == to.NodeId);
        }
    }
}