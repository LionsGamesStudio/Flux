using System.Collections.Generic;

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

        public ExecutionToken(FluxNodeBase startNode)
        {
            TargetNode = startNode;
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