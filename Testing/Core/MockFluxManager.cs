using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Configuration;
using FluxFramework.Attributes;
using FluxFramework.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FluxFramework.Testing
{
    /// <summary>
    /// A mock implementation of IFluxManager for use in unit and integration tests.
    /// It provides in-memory, non-MonoBehaviour instances of all core framework services,
    /// allowing tests to run outside of Play Mode without a live scene.
    /// </summary>
    public class MockFluxManager : IFluxManager
    {
        public bool IsInitialized { get; private set; }

        // --- Mocked Service Implementations ---
        public IEventBus EventBus { get; private set; }
        public IFluxPropertyManager Properties { get; private set; }
        public IFluxComponentRegistry Registry { get; private set; }
        public IReactiveBindingSystem BindingSystem { get; private set; }
        public IValueConverterRegistry ValueConverterRegistry { get; private set; }
        public IFluxConfigurationManager ConfigurationManager { get; private set; }
        public IFluxPersistenceManager PersistenceManager { get; private set; }
        public IFluxPropertyFactory PropertyFactory { get; private set; }
        public IFluxThreadManager Threading { get; private set; }
        public IFluxLogger Logger { get; private set; }

        public MockFluxManager()
        {
            // Instantiate all services in the correct dependency order.
            // This mirrors the constructor of the real FluxManager.
            Logger = new FluxLogger();
            Threading = new MockThreadManager(); // A version that executes actions immediately.
            Properties = new FluxPropertyManager();
            PersistenceManager = new MockPersistenceManager(); // A version that doesn't save to PlayerPrefs.
            PropertyFactory = new FluxPropertyFactory(Properties, PersistenceManager);
            EventBus = new EventBus(Threading);
            Registry = new FluxComponentRegistry(this);
            BindingSystem = new ReactiveBindingSystem(this);
            ValueConverterRegistry = new ValueConverterRegistry();
            ConfigurationManager = new MockConfigurationManager();

            // Initialize services
            EventBus.Initialize();
            Registry.Initialize();
            BindingSystem.Initialize();
            ValueConverterRegistry.Initialize();
            ConfigurationManager.Initialize();
            
            IsInitialized = true;
        }

        // Coroutines are not supported in this mock.
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public void StopCoroutine(Coroutine routine) { }

        // --- MOCK SERVICE SUBCLASSES ---

        /// <summary>Simple thread manager that executes actions immediately.</summary>
        private class MockThreadManager : IFluxThreadManager
        {
            public void ExecuteOnMainThread(System.Action action) => action?.Invoke();
            public bool IsMainThread() => true;
            public void SetMaxActionsPerFrame(int maxActions) { }
        }

        /// <summary>Persistence manager that does nothing, preventing tests from writing to PlayerPrefs.</summary>
        private class MockPersistenceManager : IFluxPersistenceManager
        {
            public void RegisterPersistentProperty(string key, IReactiveProperty property) { }
            public void LoadAllRegisteredProperties() { }
            public void SaveAll() { }
            public void Shutdown() { }
        }

        /// <summary>Configuration manager that does nothing, isolating tests from project assets.</summary>
        private class MockConfigurationManager : IFluxConfigurationManager
        {
            public void Initialize() { }

            public T GetConfiguration<T>() where T : FluxConfigurationAsset => null;

            public FluxConfigurationAsset GetConfiguration(Type configurationType) => null;

            public List<FluxConfigurationAsset> GetConfigurationsByCategory(string category) => new List<FluxConfigurationAsset>();

            public void RegisterConfiguration(FluxConfigurationAsset configuration) { }

            public void ApplyAllConfigurations(IFluxManager manager) { }

            public bool ValidateAllConfigurations() => true;

            public Dictionary<Type, FluxConfigurationAttribute> GetConfigurationTypes() => new Dictionary<Type, FluxConfigurationAttribute>();

            public void ClearCache() { }
        }
    }
}