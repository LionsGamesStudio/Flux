using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using FluxFramework.Configuration;
using FluxFramework.Events;
using FluxFramework.Binding;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FluxFramework.Testing")]

namespace FluxFramework.Core
{
    /// <summary>
    /// Core framework manager that handles initialization, thread-safe operations, and lifecycle management
    /// </summary>
    public class FluxManager : MonoBehaviour, IFluxManager
    {
        private static FluxManager _instance;
        private static readonly object _lock = new object();

        private readonly FluxThreadManager _threadManager;
        private readonly EventBus _eventBus;
        private readonly FluxPersistenceManager _persistenceManager;
        private readonly FluxPropertyManager _propertyManager;
        private readonly FluxPropertyFactory _propertyFactory;
        private readonly FluxComponentRegistry _registry;
        private readonly ReactiveBindingSystem _bindingSystem;
        private readonly ValueConverterRegistry _valueConverterRegistry;
        private readonly FluxConfigurationManager _configurationManager;

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
        /// Thread manager for main thread operations
        /// </summary>
        public IFluxThreadManager Threading => _threadManager;

        /// <summary>
        /// Event bus for decoupled communication
        /// </summary>
        public IEventBus EventBus => _eventBus;

        /// <summary>
        /// Persistence manager for saving and loading persistent properties
        /// </summary>
        public IFluxPersistenceManager PersistenceManager => _persistenceManager;

        /// <summary>
        /// Property manager for reactive properties
        /// </summary>
        public IFluxPropertyManager Properties => _propertyManager;

        /// <summary>
        /// Property factory for creating and registering reactive properties
        /// </summary>
        public IFluxPropertyFactory PropertyFactory => _propertyFactory;

        /// <summary>
        /// Component registry for managing FluxComponents
        /// </summary>
        public IFluxComponentRegistry Registry => _registry;

        /// <summary>
        /// Reactive binding system for UI bindings
        /// </summary>
        public IReactiveBindingSystem BindingSystem => _bindingSystem;

        /// <summary>
        /// Value converter registry for type conversions in bindings
        /// </summary>
        public IValueConverterRegistry ValueConverterRegistry => _valueConverterRegistry;

        /// <summary>
        /// Configuration manager for managing configurations
        /// </summary>
        public IFluxConfigurationManager ConfigurationManager => _configurationManager;


        public FluxManager()
        {
            _threadManager = new FluxThreadManager();
            _eventBus = new EventBus(_threadManager);
            _propertyManager = new FluxPropertyManager();
            _persistenceManager = new FluxPersistenceManager(_propertyManager);
            _propertyFactory = new FluxPropertyFactory(_propertyManager, _persistenceManager);
            _registry = new FluxComponentRegistry(this);
            _bindingSystem = new ReactiveBindingSystem(this);
            _valueConverterRegistry = new ValueConverterRegistry();
            _configurationManager = new FluxConfigurationManager();
        }

        /// <summary>
        /// Initializes the Flux Framework automatically
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeFramework()
        {
            if (_instance != null) return;

            float startTime = Time.realtimeSinceStartup;

            var go = new GameObject("[FluxFramework]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FluxManager>();

            Flux.Manager = _instance;

            // Add the auto-registrar for runtime component detection
            go.AddComponent<FluxComponentAutoRegistrar>();

            _instance.Initialize();

            float initTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            _instance.EventBus.Publish(new FrameworkInitializedEvent("3.0.0", true, (long)initTime));
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // Initialize configuration system first to load any saved settings
            _configurationManager.Initialize();
            _configurationManager.ApplyAllConfigurations(this);

            // Initialize core systems
            _threadManager.Initialize();
            _eventBus.Initialize();

            // Initialize all converters in the registry
            _valueConverterRegistry.Initialize();

            // Initialize component registry and discover all FluxComponent types
            _registry.Initialize();

            // Initialize the reactive binding system
            _bindingSystem.Initialize();

            SceneManager.sceneLoaded += OnSceneLoaded;

            _isInitialized = true;

            Debug.Log("[FluxFramework] Framework initialized successfully");
            Flux.InvokeOnFrameworkInitialized();

            // Auto-register all existing FluxComponents in the scene
            _registry.RegisterAllComponentsInScene();
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
            _registry.ClearCache();
            _registry.ClearInstanceCache();
            _isInitialized = false;

            // Save all pending changes before the application quits
            _persistenceManager.SaveAll();
            // Unsubscribe all persistence listeners to avoid memory leaks
            _persistenceManager.Shutdown();
        }

        /// <summary>
        /// This method is called by Unity's SceneManager every time a scene finishes loading.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // We only clear properties if it's a single scene load, not additive.
            if (mode == LoadSceneMode.Single)
            {
                _bindingSystem.ClearAll();
                _propertyManager.ClearNonPersistentProperties();
                _registry.ClearInstanceCache();
            }

            // After cleaning, re-register any components that are in the newly loaded scene.
            _registry.RegisterAllComponentsInScene();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Save data when the game is backgrounded on mobile devices
            if (pauseStatus)
            {
                _persistenceManager.SaveAll();
            }
        }
    }
}
