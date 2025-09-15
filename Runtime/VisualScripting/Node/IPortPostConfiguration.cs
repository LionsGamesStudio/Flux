using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// An interface for INode classes that need to perform logic AFTER their ports
    /// (both static and dynamic) have been created by the wrapper. This is typically used
    /// to dynamically set port properties, such as probability weights, based on the node's
    /// own configuration fields.
    /// </summary>
    public interface IPortPostConfiguration
    {
        /// <summary>
        /// Called by the AttributedNodeWrapper after all ports have been generated.
        /// </summary>
        /// <param name="wrapper">A reference to the host wrapper, providing access to the newly created ports.</param>
        void PostConfigurePorts(AttributedNodeWrapper wrapper);
    }
}