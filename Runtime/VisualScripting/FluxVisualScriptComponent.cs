using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.VisualScripting.Execution;
using System.Linq;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A MonoBehaviour component that acts as a "runner" for a FluxVisualGraph.
    /// It can manage multiple concurrent execution instances of the graph, which is
    /// essential for event-driven and asynchronous graphs.
    /// </summary>
    [FluxComponent(Category = "Visual Scripting", AutoRegister = true)]
    public class FluxVisualScriptComponent : MonoBehaviour, IGraphRunner
    {
        [Header("Graph Configuration")]
        [SerializeField] private FluxVisualGraph _graph;
        [SerializeField] private bool _executeOnStart = true;
        [SerializeField] private bool _executeOnEnable = false;
        [SerializeField] private bool _autoRegisterWithFramework = true;

        [Header("Debug")]
        [SerializeField] private bool _logExecution = false;

        public FluxVisualGraph Graph { get => _graph; set => _graph = value; }

        /// <summary>
        /// Indicates if there is at least one instance of the graph currently executing.
        /// </summary>
        public bool IsExecuting => _activeExecutors.Any();

        /// <summary>
        /// The last executor instance created by this component.
        /// Used by editor tools for debugging.
        /// </summary>
        public FluxGraphExecutor LastExecutor { get; private set; }
        
        private readonly List<FluxGraphExecutor> _activeExecutors = new List<FluxGraphExecutor>();

        private void OnEnable()
        {
            if (_autoRegisterWithFramework) FluxVisualScriptingManager.Instance?.RegisterComponent(this);
            if (_executeOnEnable) ExecuteGraph();
        }

        private void OnDisable()
        {
            if (_autoRegisterWithFramework) FluxVisualScriptingManager.Instance?.UnregisterComponent(this);
            // When disabled, we should probably stop/clean up active executors.
            // This requires a "Stop" method on the executor.
        }

        private void Start()
        {
            if (_executeOnStart) ExecuteGraph();
        }
        
        /// <summary>
        /// Begins a NEW execution instance of the assigned visual script graph.
        /// </summary>
        [FluxButton("Execute Graph")]
        public void ExecuteGraph()
        {
            if (_graph == null)
            {
                if (_logExecution) Debug.LogWarning($"VisualScriptComponent on '{gameObject.name}' has no graph assigned.", this);
                return;
            }

            try
            {
                if (_logExecution) Debug.Log($"Starting new execution of graph '{_graph.name}' on '{gameObject.name}'.", this);

                var newExecutor = new FluxGraphExecutor(_graph, this);
                LastExecutor = newExecutor;
                _activeExecutors.Add(newExecutor);
                
                // We wrap the execution in a coroutine to handle completion.
                StartCoroutine(ExecutionCoroutine(newExecutor));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error starting graph execution on '{gameObject.name}': {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        private IEnumerator ExecutionCoroutine(FluxGraphExecutor executor)
        {
            executor.Execute();
            // In this model, the synchronous part is done. We can remove it from the active list.
            // Asynchronous parts will live on via their own callbacks.
            _activeExecutors.Remove(executor);
            if (_logExecution) Debug.Log($"Initial execution of a '{_graph.name}' instance has completed on '{gameObject.name}'.", this);
            yield return null;
        }

        public void ExecuteGraphDelayed(float delay)
        {
            StartCoroutine(ExecuteGraphAfterDelay(delay));
        }

        private IEnumerator ExecuteGraphAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteGraph();
        }

        #region IGraphRunner Implementation
        
        public GameObject GetContextObject()
        {
            return this.gameObject;
        }

        #endregion
    }
}