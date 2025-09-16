using System.Collections.Generic;

namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// Defines the contract for an INode that contains executable logic.
    /// This separates pure data nodes from nodes that participate in the execution flow.
    /// </summary>
    public interface IExecutableNode : INode
    {
        /// <summary>
        /// Executes the logic of the node.
        /// </summary>
        /// <param name="executor">A reference to the graph executor, allowing the node to queue new execution tokens (for async/flow control).</param>
        /// <param name="wrapper">The host wrapper for this node logic, providing access to graph-level context (like connections).</param>
        /// <param name="triggeredPortName">The name of the input port that received the execution signal.</param>
        /// <param name="dataInputs">A dictionary containing the computed values for all data input ports of the node.</param>
        void Execute(
            Execution.FluxGraphExecutor executor,
            AttributedNodeWrapper wrapper,
            string triggeredPortName,
            Dictionary<string, object> dataInputs
        );
    }
}