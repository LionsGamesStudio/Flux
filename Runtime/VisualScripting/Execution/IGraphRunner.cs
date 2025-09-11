using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// Defines the contract for an object that can run a visual script graph.
    /// This interface provides nodes with the necessary context about their execution environment,
    /// enabling asynchronous operations and interaction with the scene.
    /// </summary>
    public interface IGraphRunner
    {
        /// <summary>
        /// Gets the GameObject that is the primary context for this graph execution.
        /// This is used for operations like GetComponent, managing subscription lifecycles, etc.
        /// </summary>
        /// <returns>The context GameObject.</returns>
        GameObject GetContextObject();
    }
}