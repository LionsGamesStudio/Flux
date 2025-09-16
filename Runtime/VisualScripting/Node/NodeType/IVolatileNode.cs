namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// A marker interface for executable nodes whose output value depends on an external state
    /// that can change during a single execution step. The executor will not cache
    /// the output of any node implementing this interface.
    /// Examples: GetFluxPropertyNode, GetRandomNumberNode.
    /// </summary>
    public interface IVolatileNode : IExecutableNode
    {
        // This interface is a marker and has no members. Its presence is the contract.
    }
}