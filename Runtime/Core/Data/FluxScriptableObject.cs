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

#if !UNITY_EDITOR
        // --- RUNTIME-ONLY LOGIC ---
        // This OnEnable block is the primary initialization method in a final build.
        // It is explicitly excluded from editor compilation to prevent any conflicts or
        // race conditions with the FluxScriptableObjectRegistry, which is the sole
        // authority for initialization within the editor.

        /// <summary>
        /// Called when the ScriptableObject is enabled by Unity.
        /// This ensures initialization in builds or for runtime-instantiated objects.
        /// </summary>
        private void OnEnable()
        {
            if (Application.isPlaying && !_isInitialized)
            {
                if (Flux.Manager != null && Flux.Manager.IsInitialized)
                {
                    InitializeNow(Flux.Manager);
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
            InitializeNow(Flux.Manager);
        }
#endif

        /// <summary>
        /// The central initialization method. It is called by the OnEnable flow (in builds)
        /// or directly by the FluxScriptableObjectRegistry (in the editor).
        /// </summary>
        private void InitializeNow(IFluxManager manager)
        {
            if (_isInitialized) return;

            if (manager == null)
            {
                Debug.LogError($"[FluxFramework] Manager is null. Cannot initialize properties for '{this.name}'.", this);
                return;
            }

            var registeredKeys = manager.PropertyFactory.RegisterPropertiesFor(this);
            _registeredProperties = new List<string>(registeredKeys);
            
            // Mark as initialized only after a successful registration.
            _isInitialized = true;

            // Call user-overridable lifecycle hooks.
            OnFluxPropertiesInitialized();
            OnFluxEnabled();
        }

        /// <summary>
        /// Public entry point for the IFluxReactiveObject interface.
        /// This is called by the FluxScriptableObjectRegistry in the editor.
        /// </summary>
        public void InitializeReactiveProperties(IFluxManager manager)
        {
            InitializeNow(manager);
        }
        
        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
#if !UNITY_EDITOR
                // Unsubscribe from the event in case this object is destroyed before the framework initializes.
                Flux.OnFrameworkInitialized -= InitializeOnce;
#endif
                OnFluxDestroy();
                CleanupReactiveProperties();
            }
        }
        
        /// <summary>
        /// Unregisters all reactive properties associated with this ScriptableObject to prevent memory leaks.
        /// </summary>
        private void CleanupReactiveProperties()
        {
            // Safety check for shutdown order.
            if (Flux.Manager == null || Flux.Manager.Properties == null || _registeredProperties.Count == 0) return;
            
            foreach (var propertyKey in _registeredProperties)
            {
                Flux.Manager.Properties.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
            _isInitialized = false; // Reset the flag
            
            OnFluxPropertiesCleanup();
        }
        
        #region User Lifecycle Hooks
        
        /// <summary>
        /// Override this method instead of OnEnable() for your initialization logic.
        /// Guaranteed to be called AFTER reactive properties are registered.
        /// </summary>
        protected virtual void OnFluxEnabled() { }

        /// <summary>
        /// Override this method instead of OnDestroy() for your cleanup logic.
        /// </summary>
        protected virtual void OnFluxDestroy() { }

        /// <summary>
        /// A lifecycle hook called immediately after all reactive properties for this component have been initialized.
        /// </summary>
        protected virtual void OnFluxPropertiesInitialized() { }

        /// <summary>
        /// A lifecycle hook called immediately after all reactive properties for this component have been cleaned up.
        /// </summary>
        protected virtual void OnFluxPropertiesCleanup() { }

        #endregion

        #region Editor Tools

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
                if (reactiveAttr != null && Flux.Manager != null)
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