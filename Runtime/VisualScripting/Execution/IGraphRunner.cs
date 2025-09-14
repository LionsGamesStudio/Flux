using UnityEngine;

namespace FluxFramework.VisualScripting.Execution
{
    /// <summary>
    /// Defines the contract for any MonoBehaviour that can host and execute a FluxVisualGraph.
    /// It provides the execution engine with a necessary link to the Unity scene context.
    /// </summary>
    public interface IGraphRunner
    {
        /// <summary>
        /// Gets the GameObject that is the primary context for this graph's execution.
        /// This is crucial for operations like starting coroutines or accessing other components.
        /// </summary>
        /// <returns>The context GameObject.</returns>
        GameObject GetContextObject();
    }
}