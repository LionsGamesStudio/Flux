using System.Collections.Generic;

namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// An interface for INode classes that need to dynamically configure their ports
    /// after they have been created, for example, to set probability weights.
    /// </summary>
    public interface IPortConfiguration
    {
        /// <summary>
        /// Gets the list of dynamic ports this node wants to create.
        /// This is called by the wrapper to build the node's final port structure.
        /// </summary>
        IEnumerable<CustomPortDefinition> GetDynamicPorts();
    }
}