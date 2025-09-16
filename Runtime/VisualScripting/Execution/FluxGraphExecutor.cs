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
    /// It operates as an asynchronous command processor, using a coroutine to process
    /// a queue of ExecutionTokens over multiple frames.
    /// </summary>
    public class FluxGraphExecutor
    {
        private readonly FluxVisualGraph _mainGraph;
        public IGraphRunner Runner { get; }

        private readonly Queue<ExecutionToken> _executionQueue = new Queue<ExecutionToken>();
        private readonly Dictionary<string, object> _nodeOutputCache = new Dictionary<string, object>();
        private Coroutine _executionCoroutine;

        public FluxGraphExecutor(FluxVisualGraph graph, IGraphRunner runner)
        {
            _mainGraph = graph ?? throw new ArgumentNullException(nameof(graph));
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            InitializeNodeGraphReferences(_mainGraph);
        }

        /// <summary>
        /// Recursively sets the ParentGraph property on all AttributedNodeWrappers.
        /// This is crucial for nodes to find their connections at runtime.
        /// </summary>
        private void InitializeNodeGraphReferences(FluxVisualGraph graph)
        {
            if (graph == null) return;
            foreach(var node in graph.Nodes)
            {
                if(node is AttributedNodeWrapper wrapper)
                {
                    wrapper.ParentGraph = graph;
                    if(wrapper.NodeLogic is SubGraphNode subGraphNode && subGraphNode.subGraph != null)
                    {
                        InitializeNodeGraphReferences(subGraphNode.subGraph);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the graph by launching the execution loop coroutine.
        /// </summary>
        public void Start()
        {
            if (Runner is not MonoBehaviour runnerMono)
            {
                Debug.LogError("Graph Runner must be a MonoBehaviour to start execution.", (UnityEngine.Object)Runner);
                return;
            }
            Stop(); // Stop any previous execution
            _executionCoroutine = runnerMono.StartCoroutine(ExecutionLoop());
        }

        /// <summary>
        /// Stops the graph execution coroutine and clears the queue.
        /// </summary>
        public void Stop()
        {
            if (_executionCoroutine != null && Runner is MonoBehaviour runnerMono)
            {
                runnerMono.StopCoroutine(_executionCoroutine);
            }
            _executionCoroutine = null;
            _executionQueue.Clear();
        }

        /// <summary>
        /// The main execution loop, running as a coroutine. It processes a batch of tokens
        /// each frame, preventing infinite loops and allowing for asynchronous operations.
        /// </summary>
        private IEnumerator ExecutionLoop()
        {
            _executionQueue.Clear();
            
            // Call OnGraphAwake on all nodes that need initialization.
            foreach (var node in _mainGraph.Nodes)
            {
                if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is IGraphAwakeNode awakeNode)
                {
                    awakeNode.OnGraphAwake(this, wrapper);
                }
            }
            
            // Enqueue initial tokens from all entry point nodes.
            foreach (var node in FindEntryNodes(_mainGraph))
            {
                _executionQueue.Enqueue(new ExecutionToken(node));
            }

            // This loop runs for the lifetime of the graph execution.
            while (true)
            {
                // Process all tokens that are currently in the queue for this frame.
                int tokensToProcess = _executionQueue.Count;
                if (tokensToProcess > 0)
                {
                    // Clear the data cache at the start of each new "tick" or "wave" of execution.
                    // This ensures state changes from one token are visible to the next.
                    _nodeOutputCache.Clear(); 
                    
                    for (int i = 0; i < tokensToProcess; i++)
                    {
                        if (_executionQueue.Count == 0) break;
                        
                        var token = _executionQueue.Dequeue();
                        if (token?.TargetNode != null)
                        {
                            ExecuteNode(token); // This method is now void and enqueues new tokens.
                        }
                    }
                }
                
                // Wait for the next frame before checking the queue again.
                yield return null; 
            }
        }
        
        /// <summary>
        /// (Public API for Nodes) Enqueues a new token for the executor to process.
        /// </summary>
        public void ContinueFlow(ExecutionToken token)
        {
            if (token != null)
            {
                _executionQueue.Enqueue(token);
            }
        }
        
        /// <summary>
        /// Executes a single node's logic and enqueues any subsequent tokens. This method is now void
        /// and acts as a command generator for the main execution loop.
        /// </summary>
        private void ExecuteNode(ExecutionToken token)
        {
            var node = token.TargetNode;
            if (node == null) return;

            #if UNITY_EDITOR
            GraphDebugger.NodeEnter(node);
            #endif

            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                var activeGraph = wrapper.ParentGraph;
                var dataInputs = PullDataForNode(node, token, activeGraph);
                string triggeredPortName = token.GetData<string>("_triggeredPortName");

                // --- Sub-graph logic ---
                if (logic is SubGraphNode subGraphNode && subGraphNode.subGraph != null)
                {
                    var entryPointNode = subGraphNode.subGraph.Nodes.OfType<AttributedNodeWrapper>().FirstOrDefault(w => w.NodeLogic is GraphInputNode);
                    if (entryPointNode != null)
                    {
                        var subGraphToken = new ExecutionToken(entryPointNode, token);
                        subGraphToken.CallStack.Push(wrapper);
                        subGraphToken.SetData("_triggeredPortName", triggeredPortName);
                        foreach (var (key, value) in dataInputs) { subGraphToken.SetData(key, value); }
                        ContinueFlow(subGraphToken);
                    }
                }
                else if (logic is GraphOutputNode)
                {
                    if (token.CallStack.TryPop(out var parentSubGraphNode))
                    {
                        var nextNodeInParent = parentSubGraphNode.GetConnectedNode(triggeredPortName);
                        if (nextNodeInParent != null)
                        {
                            var parentToken = new ExecutionToken(nextNodeInParent, token);
                            foreach (var (key, value) in dataInputs) { parentToken.SetData(key, value); }
                            ContinueFlow(parentToken);
                        }
                    }
                }
                else if (logic is IExecutableNode executableLogic)
                {
                    ApplyDataToLogic(executableLogic, node, token, dataInputs);
                    executableLogic.Execute(this, wrapper, triggeredPortName, dataInputs);
                }

                #if UNITY_EDITOR
                var dataSnapshot = BuildDataSnapshot(logic, node);
                GraphDebugger.UpdateNodeData(node, dataSnapshot);
                GraphDebugger.NodeExit(node);
                #endif
                
                // --- FLOW CONTINUATION LOGIC ---
                if (logic is IFlowControlNode)
                {
                    return; // The node is responsible for queuing new tokens itself.
                }

                var executionOutputPorts = node.OutputPorts.Where(p => p.PortType == FluxPortType.Execution);
                foreach (var outputPort in executionOutputPorts)
                {
                    foreach (var nextToken in FindNextTokensFromPorts(node, activeGraph, outputPort.Name))
                    {
                        ContinueFlow(nextToken);
                    }
                }
            }
        }

        /// <summary>
        /// Applies data inputs to a node's logic by setting its fields.
        /// Priority: 1. Direct port connections, 2. Pushed data from the execution token.
        /// </summary>
        private void ApplyDataToLogic(INode logic, FluxNodeBase node, ExecutionToken token, Dictionary<string, object> dataInputs)
        {
            var logicType = logic.GetType();

            foreach (var (portName, portValue) in dataInputs)
            {
                var field = logicType.GetField(portName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(logic, portValue);
            }

            var fields = logicType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                object tokenData = token.GetData<object>(field.Name);
                if (tokenData != null)
                {
                    var isInputPort = node.InputPorts.Any(p => p.Name == field.Name);
                    if (!isInputPort || !dataInputs.ContainsKey(field.Name))
                    {
                        try
                        {
                            var convertedValue = Convert.ChangeType(tokenData, field.FieldType);
                            field.SetValue(logic, convertedValue);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[FluxExecutor] Could not apply token data '{field.Name}' to node. Type mismatch? Error: {e.Message}", (UnityEngine.Object)node);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gathers data inputs for a node by recursively traversing its connected data input ports.
        /// </summary>
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
        /// Uses "Just-In-Time Execution" to compute the value if it's not already cached for this step.
        /// </summary>
        private object GetOutputValue(FluxNodeBase node, string portName, FluxVisualGraph currentGraph, ExecutionToken requestingToken)
        {
            bool isVolatile = (node as AttributedNodeWrapper)?.NodeLogic is IVolatileNode;
            string cacheKey = $"{node.NodeId}.{portName}";
            if (!isVolatile && _nodeOutputCache.TryGetValue(cacheKey, out object cachedValue))
            {
                return cachedValue;
            }

            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic is INode logic)
            {
                if (logic is GraphInputNode)
                {
                    var value = requestingToken.GetData<object>(portName);
                    if(!isVolatile) _nodeOutputCache[cacheKey] = value;
                    return value;
                }

                if (logic is IFlowControlNode)
                {
                    var field = logic.GetType().GetField(portName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var value = field.GetValue(logic);
                        if(!isVolatile) _nodeOutputCache[cacheKey] = value;
                        return value;
                    }
                    return null;
                }
            }
            
            if (node is AttributedNodeWrapper dataWrapper && dataWrapper.NodeLogic is IExecutableNode executableDataNode)
            {
                var dataInputsForSourceNode = PullDataForNode(node, requestingToken, currentGraph);
                ApplyDataToLogic(executableDataNode, node, requestingToken, dataInputsForSourceNode);
                executableDataNode.Execute(this, dataWrapper, null, dataInputsForSourceNode);

                var outputField = executableDataNode.GetType().GetField(portName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (outputField != null)
                {
                    var finalValue = outputField.GetValue(executableDataNode);
                    if(!isVolatile) _nodeOutputCache[cacheKey] = finalValue;
                    return finalValue;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Finds new execution tokens based on connections from a specific output port name.
        /// </summary>
        private IEnumerable<ExecutionToken> FindNextTokensFromPorts(FluxNodeBase completedNode, FluxVisualGraph activeGraph, string outputPortName)
        {
            var outputPort = completedNode.OutputPorts.FirstOrDefault(p => p.Name == outputPortName && p.PortType == FluxPortType.Execution);
            if (outputPort == null) yield break;

            var connections = activeGraph.Connections.Where(c => c.FromNodeId == completedNode.NodeId && c.FromPortName == outputPort.Name);
            foreach (var connection in connections)
            {
                var nextNode = activeGraph.Nodes.FirstOrDefault(n => n.NodeId == connection.ToNodeId);
                if (nextNode != null)
                {
                    var newToken = new ExecutionToken(nextNode);
                    newToken.SetData("_triggeredPortName", connection.ToPortName);
                    yield return newToken;
                }
            }
        }
        
        /// <summary>
        /// Finds all entry point nodes in the graph.
        /// </summary>
        private IEnumerable<FluxNodeBase> FindEntryNodes(FluxVisualGraph graph)
        {
            var connectedInputPorts = graph.Connections
                .Select(c => (c.ToNodeId, c.ToPortName))
                .ToHashSet();

            foreach (var node in graph.Nodes)
            {
                var logic = (node as AttributedNodeWrapper)?.NodeLogic;
                if(logic is null) continue;
                
                var executionInputs = node.InputPorts.Where(p => p.PortType == FluxPortType.Execution).ToList();
                if (!executionInputs.Any())
                {
                     if (logic is not IGraphAwakeNode)
                     {
                        yield return node;
                     }
                }
                else if (!executionInputs.Any(p => connectedInputPorts.Contains((node.NodeId, p.Name))))
                {
                    yield return node;
                }
            }
        }
        
        #if UNITY_EDITOR
        private FluxNodeConnection FindConnectionBetween(FluxNodeBase from, FluxNodeBase to)
        {
            if (from == null || to == null) return null;
            var fromGraph = (from as AttributedNodeWrapper)?.ParentGraph;
            if (fromGraph == null) return null;
            return fromGraph.Connections.FirstOrDefault(c => c.FromNodeId == from.NodeId && c.ToNodeId == to.NodeId);
        }

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