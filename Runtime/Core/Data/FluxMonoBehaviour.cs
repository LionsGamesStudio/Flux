using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Extensions;
using System;
using System.Collections.Generic;

namespace FluxFramework.Core
{
    /// <summary>
    /// The base class for all Flux-aware MonoBehaviours. It provides a safe, framework-aware
    /// lifecycle and implements IFluxReactiveObject to enable reactive property extensions.
    /// </summary>
    [FluxComponent(AutoRegister = true)]
    public abstract class FluxMonoBehaviour : MonoBehaviour, IFluxReactiveObject
    {
        private bool _isFrameworkInitialized = false;

        // Stores the keys of properties registered by this component for automatic cleanup.
        [NonSerialized]
        private List<string> _registeredProperties = new List<string>();

        #region Sealed Unity Lifecycle (DO NOT OVERRIDE)

        /// <summary>
        /// This Awake method is sealed by being non-virtual. It handles the core initialization logic.
        /// DO NOT declare a new Awake() method in child classes. Use OnFluxAwake() instead.
        /// </summary>
        protected void Awake()
        {
            if (Flux.Manager != null && Flux.Manager.IsInitialized)
            {
                FrameworkInitialize();
            }
            else
            {
                Flux.OnFrameworkInitialized += FrameworkInitializeOnce;
            }
        }

        /// <summary>
        /// This Start method is sealed by being non-virtual.
        /// DO NOT declare a new Start() method in child classes. Use OnFluxStart() instead.
        /// </summary>
        protected void Start() { /* Intentionally empty */ }

        /// <summary>
        /// This OnDestroy method is sealed by being non-virtual. It handles core cleanup.
        /// DO NOT declare a new OnDestroy() method in child classes. Use OnFluxDestroy() instead.
        /// </summary>
        protected void OnDestroy()
        {
            Flux.OnFrameworkInitialized -= FrameworkInitializeOnce;
            if (_isFrameworkInitialized)
            {
                OnFluxDestroy();
            }

            // Unregister all properties created by this component.
            CleanupReactiveProperties();
        }

        #endregion

        #region Property Management (IFluxReactiveObject Implementation)

        /// <summary>
        /// Discovers and registers all reactive properties on this component.
        /// This method is called by the FluxComponentRegistry at the correct time.
        /// </summary>
        public void InitializeReactiveProperties()
        {
            if (_registeredProperties.Count > 0) return;
            var registeredKeys = Flux.Manager.PropertyFactory.RegisterPropertiesFor(this);
            _registeredProperties = new List<string>(registeredKeys);
            OnFluxPropertiesInitialized();
        }

        /// <summary>
        /// Unregisters all reactive properties associated with this component.
        /// </summary>
        private void CleanupReactiveProperties()
        {
            if (Flux.Manager == null || Flux.Manager.Properties == null) return;
            foreach (var propertyKey in _registeredProperties)
            {
                Flux.Manager.Properties.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
        }

        #endregion

        #region Private Initialization Flow

        private void FrameworkInitializeOnce()
        {
            Flux.OnFrameworkInitialized -= FrameworkInitializeOnce;
            FrameworkInitialize();
        }

        private void FrameworkInitialize()
        {
            if (_isFrameworkInitialized) return;
            _isFrameworkInitialized = true;

            OnFluxAwake();
            OnFluxStart();
        }

        #endregion

        #region Protected Virtual (Overridable Lifecycle Methods for Child Classes)

        /// <summary>
        /// A lifecycle hook called immediately after all reactive properties for this component have been initialized by the registry.
        /// </summary>
        protected virtual void OnFluxPropertiesInitialized() { }

        /// <summary>
        /// The framework-safe equivalent of Awake(). Guaranteed to be called after properties are initialized.
        /// </summary>
        protected virtual void OnFluxAwake() { }
        
        /// <summary>
        /// The framework-safe equivalent of Start().
        /// </summary>
        protected virtual void OnFluxStart() { }
        
        /// <summary>
        /// The framework-safe equivalent of OnDestroy().
        /// </summary>
        protected virtual void OnFluxDestroy() { }

        #endregion
    }
}