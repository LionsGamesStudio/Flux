namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// A marker interface for executable nodes that manage their own execution flow.
    /// The FluxGraphExecutor will not automatically continue the flow from the outputs
    /// of a node that implements this interface. The node itself is responsible for calling
    /// executor.ContinueFlow() to proceed.
    /// Examples: Branch, ForEach, Delay, Timer.
    /// </summary>
    public interface IFlowControlNode : IExecutableNode
    {
        // This interface has no methods. Its presence is the contract.
    }
}