namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// An interface for INode classes that need to dynamically configure their ports
    /// after they have been created, for example, to set probability weights.
    /// </summary>
    public interface IPortConfiguration
    {
        void ConfigurePorts(AttributedNodeWrapper wrapper);
    }
}