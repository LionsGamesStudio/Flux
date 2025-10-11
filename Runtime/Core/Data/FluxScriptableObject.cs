using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using FluxFramework.Attributes;
using FluxFramework.Extensions;

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
            // Only run initialization logic when the application is actually playing.
            if (Application.isPlaying && !_isInitialized)
            {
                _isInitialized = true;
                
                // Call the overridable hook for child classes.
                OnFluxEnabled();
            }
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
        /// Automatically discovers and registers all reactive properties using the central factory.
        /// </summary>
        public void InitializeReactiveProperties(IFluxManager manager)
        {
            // CALL FACTORY TO REGISTER PROPERTIES
            var registeredKeys = manager.PropertyFactory.RegisterPropertiesFor(this);
            _registeredProperties = new List<string>(registeredKeys);

            // Call custom initialization hooks for child classes.
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
        /// Unregisters all reactive properties associated with this ScriptableObject from the central manager.
        /// </summary>
        private void CleanupReactiveProperties()
        {
            foreach (var propertyKey in _registeredProperties)
            {
                // We only unregister. The property manager handles the actual disposal/cleanup.
                Flux.Manager.Properties.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
            OnFluxPropertiesCleanup();
        }

        /// <summary>
        /// Helper method to update a reactive property's value.
        /// </summary>
        protected void UpdateReactiveProperty<T>(string propertyKey, T newValue)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = newValue;
            }
        }

        /// <summary>
        /// Helper method to get a reactive property's current value.
        /// </summary>
        protected T GetReactivePropertyValue<T>(string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        /// <summary>
        /// Helper method to subscribe to changes of a reactive property.
        /// </summary>
        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T> onChanged)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged);
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