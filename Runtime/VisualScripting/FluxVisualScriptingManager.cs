using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Graphs;
using FluxFramework.Attributes;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A singleton manager for the visual scripting system. It acts as a central registry for all
    /// active graph runners (FluxVisualScriptComponent) in the scene, providing an API to interact
    /// with them and gather statistics.
    /// </summary>
    [FluxComponent(Category = "Visual Scripting", AutoRegister = true)]
    public class FluxVisualScriptingManager : MonoBehaviour
    {
        private static FluxVisualScriptingManager _instance;
        private static bool _isShuttingDown = false;

        // The manager now primarily tracks the active runners, which are the context for graph execution.
        private readonly List<FluxVisualScriptComponent> _registeredComponents = new List<FluxVisualScriptComponent>();
        private readonly Dictionary<string, FluxVisualScriptComponent> _namedComponents = new Dictionary<string, FluxVisualScriptComponent>();

        /// <summary>
        /// Singleton instance of the visual scripting manager.
        /// </summary>
        public static FluxVisualScriptingManager Instance
        {
            get
            {
                if (_isShuttingDown) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<FluxVisualScriptingManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("FluxVisualScriptingManager");
                        _instance = go.AddComponent<FluxVisualScriptingManager>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// A read-only list of all currently registered graphs (derived from active components).
        /// </summary>
        public IReadOnlyList<FluxVisualGraph> RegisteredGraphs => _registeredComponents.Select(c => c.Graph).Where(g => g != null).Distinct().ToList();

        public event Action<FluxVisualGraph> OnGraphRegistered;
        public event Action<FluxVisualGraph> OnGraphUnregistered;
        public event Action<FluxVisualGraph> OnGraphExecutionStarted;
        public event Action<FluxVisualGraph> OnGraphExecutionCompleted;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeManager()
        {
            Debug.Log("[FluxFramework] Visual Scripting Manager initialized.");
        }

        /// <summary>
        /// Registers a visual script component with the manager.
        /// This is typically called from the component's OnEnable method.
        /// </summary>
        public void RegisterComponent(FluxVisualScriptComponent component)
        {
            if (component == null || _registeredComponents.Contains(component)) return;

            _registeredComponents.Add(component);

            // If the component's GameObject has a unique name, register it for easy access.
            if (!_namedComponents.ContainsKey(component.gameObject.name))
            {
                _namedComponents[component.gameObject.name] = component;
            }

            if (component.Graph != null)
            {
                component.Graph.OnExecutionStarted += HandleGraphExecutionStarted;
                component.Graph.OnExecutionCompleted += HandleGraphExecutionCompleted;
                OnGraphRegistered?.Invoke(component.Graph);
            }
        }

        /// <summary>
        /// Unregisters a visual script component from the manager.
        /// This is typically called from the component's OnDisable method.
        /// </summary>
        public void UnregisterComponent(FluxVisualScriptComponent component)
        {
            if (component == null || !_registeredComponents.Contains(component)) return;

            _registeredComponents.Remove(component);

            if (_namedComponents.ContainsKey(component.gameObject.name) && _namedComponents[component.gameObject.name] == component)
            {
                _namedComponents.Remove(component.gameObject.name);
            }

            if (component.Graph != null)
            {
                component.Graph.OnExecutionStarted -= HandleGraphExecutionStarted;
                component.Graph.OnExecutionCompleted -= HandleGraphExecutionCompleted;
                OnGraphUnregistered?.Invoke(component.Graph);
            }
        }

        /// <summary>
        /// Finds and executes the first active graph with the given name.
        /// It does so by finding the FluxVisualScriptComponent that is running the graph.
        /// </summary>
        /// <param name="graphName">The name of the FluxVisualGraph asset to execute.</param>
        public void ExecuteGraph(string graphName)
        {
            var component = _registeredComponents.FirstOrDefault(c => c.Graph != null && c.Graph.name == graphName);
            if (component != null)
            {
                Debug.Log($"[FluxVSManager] Executing graph '{graphName}' via component '{component.gameObject.name}'.", this);
                component.ExecuteGraph();
            }
            else
            {
                Debug.LogWarning($"[FluxVSManager] No active component found running a graph named '{graphName}'.", this);
            }
        }

        /// <summary>
        /// Finds and executes a component's graph by the component's GameObject name.
        /// </summary>
        /// <param name="gameObjectName">The name of the GameObject hosting the FluxVisualScriptComponent.</param>
        public void ExecuteComponent(string gameObjectName)
        {
            if (_namedComponents.TryGetValue(gameObjectName, out var component) && component != null)
            {
                component.ExecuteGraph();
            }
            else
            {
                Debug.LogWarning($"[FluxVSManager] No registered component found on a GameObject named '{gameObjectName}'.", this);
            }
        }

        /// <summary>
        /// Finds all graphs that contain at least one node of a specific type.
        /// </summary>
        public List<FluxVisualGraph> FindGraphsWithNodeType<T>() where T : FluxNodeBase
        {
            return RegisteredGraphs.Where(graph =>
                graph.FindNodes<T>().Any()).ToList();
        }

        /// <summary>
        /// Gets statistics about the current state of the visual scripting system.
        /// </summary>
        public VisualScriptingStats GetStatistics()
        {
            return new VisualScriptingStats
            {
                TotalGraphs = RegisteredGraphs.Count(),
                TotalNodes = RegisteredGraphs.Sum(g => g.Nodes.Count),
                TotalConnections = RegisteredGraphs.Sum(g => g.Connections.Count),
                RegisteredComponents = _registeredComponents.Count
            };
        }

        private void HandleGraphExecutionStarted(FluxVisualGraph graph)
        {
            OnGraphExecutionStarted?.Invoke(graph);
            if (graph != null) Debug.Log($"Graph '{graph.name}' started execution.");
        }

        private void HandleGraphExecutionCompleted(FluxVisualGraph graph)
        {
            OnGraphExecutionCompleted?.Invoke(graph);
            if (graph != null) Debug.Log($"Graph '{graph.name}' completed execution.");
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions on all components to prevent issues on scene unload.
            foreach (var component in _registeredComponents.ToList())
            {
                UnregisterComponent(component);
            }
            _registeredComponents.Clear();
            _namedComponents.Clear();
        }
        
        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        #if UNITY_EDITOR
        [FluxButton("Open Visual Scripting Editor")]
        private void OpenEditor()
        {
            var windowType = Type.GetType("FluxFramework.VisualScripting.Editor.FluxVisualScriptingWindow, FluxFramework.Editor");
            if (windowType != null)
            {
                var showWindowMethod = windowType.GetMethod("ShowWindow", BindingFlags.Public | BindingFlags.Static);
                showWindowMethod?.Invoke(null, null);
            }
            else
            {
                Debug.LogWarning("FluxVisualScriptingWindow type not found. Make sure the Editor assembly is loaded.");
            }
        }

        [FluxButton("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"--- Visual Scripting Statistics ---\n" +
                      $"Active Components: {stats.RegisteredComponents}\n" +
                      $"Unique Graphs: {stats.TotalGraphs}\n" +
                      $"Total Nodes (in active graphs): {stats.TotalNodes}\n" +
                      $"Total Connections (in active graphs): {stats.TotalConnections}", this);
        }
#endif
    }

    /// <summary>
    /// A struct to hold statistics about the visual scripting system.
    /// </summary>
    [Serializable]
    public struct VisualScriptingStats
    {
        public int TotalGraphs;
        public int TotalNodes;
        public int TotalConnections;
        public int RegisteredComponents;
    }
}