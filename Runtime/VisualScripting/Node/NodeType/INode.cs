namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// A marker interface for all attribute-based node logic classes (POCOs).
    /// Any class implementing this interface is considered a "node brain"
    /// that can be hosted inside an AttributedNodeWrapper.
    /// </summary>
    public interface INode
    {
        // This interface can remain empty for now. Its presence is the contract.
        // In the future, it could contain a method like 'OnGraphStart()' if needed.
    }
}