using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base class for ScriptableObjects that integrate with the Flux Framework.
    /// It automatically handles reactive property registration and cleanup.
    /// </summary>
    public abstract class FluxScriptableObject : ScriptableObject, IFluxReactiveObject
    {
        [NonSerialized]
        private List<string> _registeredProperties = new List<string>();
        
        [NonSerialized]
        private bool _isInitialized = false;

        /// <summary>
        /// Called when the ScriptableObject is enabled by Unity.
        /// </summary>
        private void OnEnable()
        {
            if (Application.isPlaying && !_isInitialized)
            {
                if (Flux.Manager != null && Flux.Manager.IsInitialized)
                {
                    InitializeNow();
                }
                else
                {
                    // If the framework is not ready yet, subscribe to its initialization event.
                    Flux.OnFrameworkInitialized += InitializeOnce;
                }
            }
        }
        
        private void InitializeOnce()
        {
            Flux.OnFrameworkInitialized -= InitializeOnce;
            InitializeNow();
        }

        private void InitializeNow()
        {
            if (_isInitialized) return;

            InitializeReactiveProperties(Flux.Manager);
            _isInitialized = true;
            OnFluxEnabled();
        }

        /// <summary>
        /// Override this method instead of OnEnable() for your initialization logic.
        /// </summary>
        protected virtual void OnFluxEnabled() { }

        /// <summary>
        /// Called when the ScriptableObject is about to be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            // Unsubscribe from the event in case this object is destroyed before the framework initializes.
            Flux.OnFrameworkInitialized -= InitializeOnce;
            
            if (Application.isPlaying)
            {
                OnFluxDestroy();
                CleanupReactiveProperties();
            }
        }
        
        /// <summary>
        /// Override this method instead of OnDestroy() for your cleanup logic.
        /// </summary>
        protected virtual void OnFluxDestroy() { }

        /// <summary>
        /// Automatically discovers and registers all reactive properties using the provided manager.
        /// </summary>
        public void InitializeReactiveProperties(IFluxManager manager)
        {
            if (manager == null)
            {
                Debug.LogError($"[FluxFramework] Cannot initialize properties for '{this.name}' because the provided manager is null.", this);
                return;
            }

            var registeredKeys = manager.PropertyFactory.RegisterPropertiesFor(this);
            _registeredProperties = new List<string>(registeredKeys);
            OnFluxPropertiesInitialized();
        }

        /// <summary>
        // Caches the keys of all properties registered for this object, so they can be cleaned up OnDestroy.
        /// </summary>
        private void CacheRegisteredPropertyKeys()
        {
            _registeredProperties.Clear();
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    _registeredProperties.Add(reactiveAttr.Key);
                }
            }
        }

        /// <summary>
        /// Unregisters all reactive properties associated with this ScriptableObject.
        /// </summary>
        private void CleanupReactiveProperties()
        {
            // Safety check for shutdown order.
            if (Flux.Manager == null || Flux.Manager.Properties == null) return;
            
            foreach (var propertyKey in _registeredProperties)
            {
                Flux.Manager.Properties.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
            OnFluxPropertiesCleanup();
        }
        
        #region Lifecycle Hooks & Editor Tools
        
        protected virtual void OnFluxPropertiesInitialized() { }
        protected virtual void OnFluxPropertiesCleanup() { }

        /// <summary>
        /// Editor-only method to force initialization of reactive properties.
        /// </summary>
        [ContextMenu("Initialize Reactive Properties")]
        public void ForceInitializeReactiveProperties()
        {
            if (!_isInitialized)
            {
                InitializeReactiveProperties(Flux.Manager);
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Editor-only method to reset all reactive properties to their default values as defined in their attributes.
        /// </summary>
        [ContextMenu("Reset Reactive Properties")]
        public virtual void ResetReactiveProperties()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var propertyKey = reactiveAttr.Key;
                    var defaultValue = reactiveAttr.DefaultValue ?? GetDefaultValue(field.FieldType);
                    field.SetValue(this, defaultValue);
                    var property = Flux.Manager.Properties.GetProperty(propertyKey);
                    property?.SetValue(defaultValue);
                }
            }
        }
        
        private object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        #endregion
    }
}