using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base class for ScriptableObjects that integrate with the Flux Framework
    /// Automatically handles reactive property registration and cleanup
    /// </summary>
    public abstract class FluxScriptableObject : ScriptableObject, IFluxReactiveObject
    {
        [System.NonSerialized]
        private List<string> _registeredProperties = new List<string>();
        
        [System.NonSerialized]
        private bool _isInitialized = false;

        /// <summary>
        /// Called when the ScriptableObject is enabled
        /// </summary>
        private void OnEnable()
        {
            if (Application.isPlaying && !_isInitialized)
            {
                InitializeReactiveProperties();
                _isInitialized = true;
                
                // Call the overridable method for child classes
                OnFluxEnabled();
            }
        }
        
        /// <summary>
        /// Override this method instead of OnEnable() for your initialization logic
        /// </summary>
        protected virtual void OnFluxEnabled()
        {
            // Child classes can override this
        }

        /// <summary>
        /// Called when the ScriptableObject is being destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                // Call the overridable method first
                OnFluxDestroy();
                
                // Then ensure cleanup happens
                CleanupReactiveProperties();
            }
        }
        
        /// <summary>
        /// Override this method instead of OnDestroy() for your cleanup logic
        /// </summary>
        protected virtual void OnFluxDestroy()
        {
            // Child classes can override this
        }

        /// <summary>
        /// Automatically discovers and registers all reactive properties using reflection
        /// </summary>
        public void InitializeReactiveProperties()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    RegisterReactiveField(field, reactiveAttr);
                }
            }
            
            // Call custom initialization
            OnReactivePropertiesInitialized();
            
            // Call the overridable method for child classes
            OnFluxPropertiesInitialized();
        }
        
        /// <summary>
        /// Override this method for custom initialization logic after reactive properties are set up
        /// </summary>
        protected virtual void OnFluxPropertiesInitialized()
        {
            // Child classes can override this
        }

        /// <summary>
        /// Registers a single field as a reactive property
        /// </summary>
        private void RegisterReactiveField(FieldInfo field, ReactivePropertyAttribute attribute)
        {
            var propertyKey = attribute.Key;
            var fieldValue = field.GetValue(this);
            
            IReactiveProperty reactiveProperty = null;
            
            try
            {
                var propertyType = typeof(ReactiveProperty<>).MakeGenericType(field.FieldType);
                reactiveProperty = (IReactiveProperty)Activator.CreateInstance(propertyType, fieldValue);

                reactiveProperty.Subscribe(newValue => field.SetValue(this, newValue));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Could not create reactive property for field '{field.Name}' in '{this.name}': {ex.Message}", this);
                return;
            }

            if (reactiveProperty != null)
            {
                // Persistent flag is now handled
                FluxManager.Instance.RegisterProperty(propertyKey, reactiveProperty, attribute.Persistent);
                _registeredProperties.Add(propertyKey);
            }
        }

        /// <summary>
        /// Cleanup all registered reactive properties
        /// </summary>
        private void CleanupReactiveProperties()
        {
            foreach (var propertyKey in _registeredProperties)
            {
                FluxManager.Instance.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
            
            OnReactivePropertiesCleanup();
        }

        /// <summary>
        /// Updates a reactive property value and synchronizes with the field
        /// </summary>
        protected void UpdateReactiveProperty<T>(string propertyKey, T newValue)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = newValue;
            }
        }

        /// <summary>
        /// Gets the current value of a reactive property
        /// </summary>
        protected T GetReactivePropertyValue<T>(string propertyKey)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        /// <summary>
        /// Subscribe to changes of a reactive property
        /// </summary>
        protected void SubscribeToProperty<T>(string propertyKey, System.Action<T> onChanged)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            property?.Subscribe(onChanged);
        }

        /// <summary>
        /// Override this method to perform custom initialization after reactive properties are set up
        /// </summary>
        protected virtual void OnReactivePropertiesInitialized() { }

        /// <summary>
        /// Override this method to perform custom cleanup when reactive properties are being destroyed
        /// </summary>
        protected virtual void OnReactivePropertiesCleanup() { }

        /// <summary>
        /// Force initialization of reactive properties (useful for testing or manual setup)
        /// </summary>
        [ContextMenu("Initialize Reactive Properties")]
        public void ForceInitializeReactiveProperties()
        {
            if (!_isInitialized)
            {
                InitializeReactiveProperties();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Reset all reactive properties to their default values
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
                    
                    // Get default value from attribute or field
                    var defaultValue = reactiveAttr.DefaultValue ?? GetDefaultValue(field.FieldType);
                    field.SetValue(this, defaultValue);
                    
                    // Update reactive property
                    var property = FluxManager.Instance.GetProperty(propertyKey);
                    if (property != null)
                    {
                        // Use reflection to set the value
                        var valueProperty = property.GetType().GetProperty("Value");
                        valueProperty?.SetValue(property, defaultValue);
                    }
                }
            }
        }

        /// <summary>
        /// Get default value for a type
        /// </summary>
        private object GetDefaultValue(System.Type type)
        {
            if (type.IsValueType)
            {
                return System.Activator.CreateInstance(type);
            }
            return null;
        }
    }

    /// <summary>
    /// Interface for objects that have reactive properties
    /// </summary>
    public interface IFluxReactiveObject
    {
        void InitializeReactiveProperties();
    }
}
