using System;
using System.Collections.Generic;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// A static class that acts as a bridge between the runtime graph executor
    /// and the editor's graph view for visual debugging purposes.
    /// This should only be used in the Unity Editor.
    /// </summary>
    public static class GraphDebugger
    {
        #if UNITY_EDITOR
        
        /// <summary>
        /// Fired when a node begins its execution.
        /// The string is the NodeId.
        /// </summary>
        public static event Action<string> OnNodeEnter;

        /// <summary>
        /// Fired after a node executes, carrying a snapshot of all its data port values.
        /// The string is the NodeId. The Dictionary maps Port Names to their string representation.
        /// </summary>
        public static event Action<string, Dictionary<string, string>> OnNodeDataUpdate;
        
        /// <summary>
        /// Fired when a node has finished its execution.
        /// The string is the NodeId.
        /// </summary>
        public static event Action<string> OnNodeExit;

        /// <summary>
        /// Fired when an execution token travels across a connection.
        /// The strings are the FromNodeId, FromPortName, ToNodeId, ToPortName.
        /// </summary>
        public static event Action<string, string, string, string> OnTokenTraverse;

        // --- Methods for the Executor to call ---

        public static void NodeEnter(FluxNodeBase node) => OnNodeEnter?.Invoke(node.NodeId);
        public static void NodeExit(FluxNodeBase node) => OnNodeExit?.Invoke(node.NodeId);
        public static void TokenTraverse(string fromNodeId, string fromPortName, string toNodeId, string toPortName) => 
            OnTokenTraverse?.Invoke(fromNodeId, fromPortName, toNodeId, toPortName);
        public static void UpdateNodeData(FluxNodeBase node, Dictionary<string, string> portValues) => 
            OnNodeDataUpdate?.Invoke(node.NodeId, portValues);
        
        #endif
    }
}