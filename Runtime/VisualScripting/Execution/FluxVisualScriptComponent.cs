using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A MonoBehaviour that hosts and runs a FluxVisualGraph.
    /// It acts as the bridge between the graph data asset and the Unity scene.
    /// </summary>
    [AddComponentMenu("Flux/Visual Script Runner")]
    public class FluxVisualScriptComponent : MonoBehaviour, IGraphRunner
    {
        [Tooltip("The visual script graph asset to execute.")]
        [SerializeField] private FluxVisualGraph _graph;
        
        [Tooltip("If true, the graph will automatically start executing in the Start() method.")]
        [SerializeField] private bool _executeOnStart = true;
        
        // The runtime instance of the executor for this component.
        private FluxGraphExecutor _executor;
        
        void Start()
        {
            if (_graph == null)
            {
                Debug.LogWarning("FluxVisualScriptComponent has no graph assigned.", this);
                return;
            }
            
            // Create a new execution instance for this graph.
            _executor = new FluxGraphExecutor(_graph, this);

            if (_executeOnStart)
            {
                _executor.Start();
            }
        }

        /// <summary>
        /// Public method to manually trigger the graph's execution.
        /// </summary>
        public void Execute()
        {
            _executor?.Start();
        }

        #region IGraphRunner Implementation
        
        public GameObject GetContextObject()
        {
            return this.gameObject;
        }
        
        #endregion
    }
}