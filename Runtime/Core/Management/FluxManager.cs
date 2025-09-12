using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using FluxFramework.Configuration;
using FluxFramework.Binding;

namespace FluxFramework.Core
{
    /// <summary>
    /// Core framework manager that handles initialization, thread-safe operations, and lifecycle management
    /// </summary>
    public class FluxManager : MonoBehaviour
    {
        private static FluxManager _instance;
        private static readonly object _lock = new object();

        private readonly FluxPropertyManager _propertyManager;
        private readonly FluxThreadManager _threadManager;

        private bool _isInitialized = false;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { _isInitialized = value; }
        }

        /// <summary>
        /// Singleton instance of the FluxManager
        /// </summary>
        public static FluxManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            InitializeFramework();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Event raised when the framework is fully initialized
        /// </summary>
        public static event Action OnFrameworkInitialized;

        /// <summary>
        /// Property manager for reactive properties
        /// </summary>
        public FluxPropertyManager Properties => _propertyManager;

        /// <summary>
        /// Thread manager for main thread operations
        /// </summary>
        public FluxThreadManager Threading => _threadManager;

        public FluxManager()
        {
            _propertyManager = new FluxPropertyManager();
            _threadManager = new FluxThreadManager();
        }

        /// <summary>
        /// Initializes the Flux Framework automatically
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeFramework()
        {
            if (_instance != null) return;

            var go = new GameObject("[FluxFramework]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FluxManager>();

            // Add the auto-registrar for runtime component detection
            go.AddComponent<FluxComponentAutoRegistrar>();

            _instance.Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Initialize configuration system first
            FluxConfigurationManager.Initialize();
            FluxConfigurationManager.ApplyAllConfigurations();

            // Initialize all converters in the registry
            ValueConverterRegistry.Initialize();

            // Initialize component registry and discover all FluxComponent types
            FluxComponentRegistry.Initialize();

            _threadManager.Initialize();

            // Initialize all framework systems
            FluxFramework.Binding.ReactiveBindingSystem.Initialize();
            EventBus.Initialize();

            _isInitialized = true;

            Debug.Log("[FluxFramework] Framework initialized successfully");
            OnFrameworkInitialized?.Invoke();

            // Auto-register all existing FluxComponents in the scene
            FluxComponentRegistry.RegisterAllComponentsInScene();
        }

        private void Update()
        {
            _threadManager.ProcessMainThreadActions();
        }

        /// <summary>
        /// Executes an action on the main thread in a thread-safe manner
        /// </summary>
        /// <param name="action">Action to execute on main thread</param>
        public void ExecuteOnMainThread(Action action)
        {
            _threadManager.ExecuteOnMainThread(action);
        }

        /// <summary>
        /// Registers a reactive property with the framework
        /// </summary>
        /// <param name="key">Unique key for the property</param>
        /// <param name="property">Reactive property instance</param>
        public void RegisterProperty(string key, IReactiveProperty property)
        {
            _propertyManager.RegisterProperty(key, property);
        }

        /// <summary>
        /// Gets a reactive property by key
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="key">Property key</param>
        /// <returns>Reactive property or null if not found</returns>
        public ReactiveProperty<T> GetProperty<T>(string key)
        {
            return _propertyManager.GetProperty<T>(key);
        }

        /// <summary>
        /// Gets or creates a reactive property
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if property doesn't exist</param>
        /// <returns>Reactive property</returns>
        public ReactiveProperty<T> GetOrCreateProperty<T>(string key, T defaultValue = default)
        {
            return _propertyManager.GetOrCreateProperty(key, defaultValue);
        }

        /// <summary>
        /// Gets a reactive property by key (non-generic version)
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Reactive property or null if not found</returns>
        public IReactiveProperty GetProperty(string key)
        {
            return _propertyManager.GetProperty(key);
        }

        /// <summary>
        /// Unregisters a reactive property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property was removed, false if not found</returns>
        public bool UnregisterProperty(string key)
        {
            return _propertyManager.UnregisterProperty(key);
        }

        /// <summary>
        /// Checks if a property exists
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property exists</returns>
        public bool HasProperty(string key)
        {
            return _propertyManager.HasProperty(key);
        }

        /// <summary>
        /// Gets all registered property keys
        /// </summary>
        /// <returns>Collection of property keys</returns>
        public IEnumerable<string> GetAllPropertyKeys()
        {
            return _propertyManager.GetAllPropertyKeys();
        }

        private void OnDestroy()
        {
            _propertyManager.Clear();
            _isInitialized = false;

            // Save all pending changes before the application quits
            FluxPersistenceManager.SaveAll();
            // Unsubscribe all persistence listeners to avoid memory leaks
            FluxPersistenceManager.Shutdown();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Save data when the game is backgrounded on mobile devices
            if (pauseStatus)
            {
                FluxPersistenceManager.SaveAll();
            }
        }
    }
}
