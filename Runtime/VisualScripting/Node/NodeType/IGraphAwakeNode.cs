namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// Defines a contract for nodes that need to perform setup logic
    /// when the graph execution begins.
    /// </summary>
    public interface IGraphAwakeNode : INode
    {
        /// <summary>
        /// Called by the FluxGraphExecutor once when the graph starts.
        /// </summary>
        /// <param name="executor">The graph executor instance.</param>
        /// <param name="wrapper">The host wrapper for this node logic.</param>
        void OnGraphAwake(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper);
    }
}