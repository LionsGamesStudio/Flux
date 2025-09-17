using System.Collections.Generic;
using System.Linq;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// Represents a single, independent "thread" of execution flowing through the graph.
    /// A token is created at an entry point and is consumed when it reaches an end point.
    /// It can carry its own local data, allowing for concurrent execution flows.
    /// </summary>
    public class ExecutionToken
    {
        /// <summary>
        /// The next node that this token is waiting to execute.
        /// </summary>
        public FluxNodeBase TargetNode { get; set; }

        /// <summary>
        /// A private data store for this token. A node can add data to a token
        /// that can be retrieved by subsequent nodes in the same execution chain.
        /// </summary>
        private Dictionary<string, object> _localData;

        /// <summary>
        /// The call stack for this token, used to manage sub-graph execution.
        /// Each entry is the SubGraphNode that was called to enter the current graph level.
        /// </summary>
        public Stack<AttributedNodeWrapper> CallStack { get; }

        public ExecutionToken(FluxNodeBase startNode)
        {
            TargetNode = startNode;
            CallStack = new Stack<AttributedNodeWrapper>();
        }

        /// <summary>
        /// Creates a new token that inherits the state (like the call stack) of a parent token.
        /// </summary>
        public ExecutionToken(FluxNodeBase startNode, ExecutionToken parentToken)
        {
            TargetNode = startNode;
            // The new token gets a COPY of the parent's call stack.
            CallStack = new Stack<AttributedNodeWrapper>(parentToken.CallStack.Reverse());
            // We also could copy local data if needed in the future.
        }
        
        /// <summary>
        /// Stores a piece of data within this specific token.
        /// </summary>
        public void SetData(string key, object value)
        {
            _localData = _localData ?? new Dictionary<string, object>();
            _localData[key] = value;
        }

        /// <summary>
        /// Retrieves a piece of data stored within this token.
        /// </summary>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_localData != null && _localData.TryGetValue(key, out var data) && data is T typedData)
            {
                return typedData;
            }
            return defaultValue;
        }
    }
}