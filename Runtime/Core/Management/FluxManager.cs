using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using FluxFramework.Configuration;
using FluxFramework.Binding;

namespace FluxFramework.Core
{
    /// <summary>
    /// Core framework manager that handles initialization, thread-safe operations, and lifecycle management
    /// </summary>
    public class FluxManager : MonoBehaviour, IFluxManager
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
        /// Property manager for reactive properties
        /// </summary>
        public IFluxPropertyManager Properties => _propertyManager;

        /// <summary>
        /// Thread manager for main thread operations
        /// </summary>
        public IFluxThreadManager Threading => _threadManager;

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
            FluxConfigurationManager.ApplyAllConfigurations(this);

            // Initialize all converters in the registry
            ValueConverterRegistry.Initialize();

            // Initialize component registry and discover all FluxComponent types
            FluxComponentRegistry.Initialize();

            _threadManager.Initialize();

            // Initialize all framework systems
            FluxFramework.Binding.ReactiveBindingSystem.Initialize();
            EventBus.Initialize();

            SceneManager.sceneLoaded += OnSceneLoaded;

            _isInitialized = true;

            Debug.Log("[FluxFramework] Framework initialized successfully");
            Flux.InvokeOnFrameworkInitialized();

            // Auto-register all existing FluxComponents in the scene
            FluxComponentRegistry.RegisterAllComponentsInScene();
        }

        private void Update()
        {
            _threadManager.ProcessMainThreadActions();
        }

        public Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);
        public void StopCoroutine(Coroutine routine) => base.StopCoroutine(routine);

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            _propertyManager.Clear();
            _isInitialized = false;

            // Save all pending changes before the application quits
            FluxPersistenceManager.SaveAll();
            // Unsubscribe all persistence listeners to avoid memory leaks
            FluxPersistenceManager.Shutdown();
        }

        /// <summary>
        /// This method is called by Unity's SceneManager every time a scene finishes loading.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // We only clear properties if it's a single scene load, not additive.
            if (mode == LoadSceneMode.Single)
            {
                ReactiveBindingSystem.ClearAll();
                _propertyManager.ClearNonPersistentProperties();
                FluxComponentRegistry.ClearInstanceCache();
            }

            // After cleaning, re-register any components that are in the newly loaded scene.
            FluxComponentRegistry.RegisterAllComponentsInScene();
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
