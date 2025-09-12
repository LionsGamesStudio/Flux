using UnityEngine;
using FluxFramework.Attributes;
using System;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FluxFramework.Core
{
    /// <summary>
    /// The base class for all Flux-aware MonoBehaviours. It provides a safe, framework-aware
    /// lifecycle. 
    /// It ensures that all Flux components are initialized only after the Flux Framework is fully set up.
    /// It also provides helper methods for interacting with reactive properties and the event bus.
    /// </summary>
    [FluxComponent(AutoRegister = true)]
    public abstract class FluxMonoBehaviour : MonoBehaviour
    {
        private bool _isFrameworkInitialized = false;

        #region Sealed Unity Lifecycle (DO NOT OVERRIDE)

        /// <summary>
        /// This Awake method is sealed by being non-virtual. It handles the core initialization logic.
        /// DO NOT declare a new Awake() method in child classes. Use OnFluxAwake() instead.
        /// </summary>
        protected void Awake()
        {
            if (FluxManager.Instance != null && FluxManager.Instance.IsInitialized)
            {
                FrameworkInitialize();
            }
            else
            {
                FluxManager.OnFrameworkInitialized += FrameworkInitializeOnce;
            }
        }

        /// <summary>
        /// This Start method is sealed by being non-virtual.
        /// DO NOT declare a new Start() method in child classes. Use OnFluxStart() instead.
        /// </summary>
        protected void Start()
        {
            // This method is intentionally left empty.
            // Its purpose is to exist so that Unity calls it, but the user-facing logic is in OnFluxStart().
        }

        /// <summary>
        /// This OnDestroy method is sealed by being non-virtual. It handles core cleanup.
        /// DO NOT declare a new OnDestroy() method in child classes. Use OnFluxDestroy() instead.
        /// </summary>
        protected void OnDestroy()
        {
            FluxManager.OnFrameworkInitialized -= FrameworkInitializeOnce;

            if (_isFrameworkInitialized)
            {
                OnFluxDestroy();
            }
        }

        #endregion

        #region Private Initialization Flow

        private void FrameworkInitializeOnce()
        {
            FluxManager.OnFrameworkInitialized -= FrameworkInitializeOnce;
            FrameworkInitialize();
        }



        private void FrameworkInitialize()
        {
            if (_isFrameworkInitialized) return;
            _isFrameworkInitialized = true;

            // Call the safe, overridable lifecycle methods for child classes.
            OnFluxAwake();
            OnFluxStart();
        }

        #endregion

        #region Protected Virtual (Overridable Lifecycle Methods for Child Classes)

        /// <summary>
        /// This is the framework-safe equivalent of Awake().
        /// It is guaranteed to be called only once and after the Flux Framework is fully initialized.
        /// Override this for component setup and property initialization/subscription.
        /// </summary>
        protected virtual void OnFluxAwake() { }

        /// <summary>
        /// This is the framework-safe equivalent of Start().
        /// It is guaranteed to be called after OnFluxAwake().
        /// Override this for logic that may depend on other components being initialized.
        /// </summary>
        protected virtual void OnFluxStart() { }

        /// <summary>
        /// This is the framework-safe equivalent of OnDestroy().
        /// Override this for all your cleanup logic, such as disposing of subscriptions.
        /// </summary>
        protected virtual void OnFluxDestroy() { }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Atomically updates a reactive property by setting it to a new value.
        /// This is the simplest way to modify a property when using the implicit pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyKey"></param>
        /// <param name="newValue"></param>
        protected void UpdateReactiveProperty<T>(string propertyKey, T newValue)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            if (property != null)
            {
                // This will trigger validation. The property's value will either update or stay the same.
                property.Value = newValue;

                // Update the local field if one is bound to this property.
                ForceSyncLocalField(propertyKey, property.Value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to update property '{propertyKey}', but it does not exist.", this);
            }
        }


        /// <summary>
        /// Atomically updates a reactive property by applying an update function to its current value.
        /// This is the safest way to modify a property when using the implicit pattern.
        /// It guarantees that you are modifying the most up-to-date, validated value.
        /// </summary>
        /// <example>
        /// UpdateReactiveProperty("player.health", currentHealth => currentHealth - 10);
        /// </example>
        protected void UpdateReactiveProperty<T>(string propertyKey, Func<T, T> updateFunction)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            if (property != null)
            {
                // This performs the safe "read -> modify -> write" cycle.
                property.Value = updateFunction(property.Value);

                ForceSyncLocalField(propertyKey, property.Value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to update property '{propertyKey}', but it does not exist.", this);
            }
        }

        /// <summary>
        /// A convenience overload to update a property you have an explicit reference to.
        /// </summary>
        protected void UpdateReactiveProperty<T>(ReactiveProperty<T> property, T newValue)
        {
            if (property != null)
            {
                property.Value = newValue;
            }
        }

        protected T GetReactivePropertyValue<T>(string propertyKey)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T> onChanged, bool fireOnSubscribe = false)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T, T> onChanged, bool fireOnSubscribe = false)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        protected void PublishEvent<T>(T eventData) where T : IFluxEvent
        {
            EventBus.Publish(eventData);
        }
        
        /// <summary>
        /// A private helper to find the local field associated with a property key and set its value.
        /// </summary>
        private void ForceSyncLocalField(string propertyKey, object managerValue)
        {
            var field = GetFieldForPropertyKey(propertyKey);
            if (field != null)
            {
                field.SetValue(this, managerValue);
            }
        }
        
        /// <summary>
        /// A cached helper to find a FieldInfo from a property key via reflection.
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<string, System.Reflection.FieldInfo>> _fieldCache = 
            new System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<string, System.Reflection.FieldInfo>>();
        
        private System.Reflection.FieldInfo GetFieldForPropertyKey(string propertyKey)
        {
            var type = this.GetType();
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = new Dictionary<string, System.Reflection.FieldInfo>();
                var allFields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var f in allFields)
                {
                    var attr = f.GetCustomAttribute<ReactivePropertyAttribute>();
                    if (attr != null)
                    {
                        fields[attr.Key] = f;
                    }
                }
                _fieldCache[type] = fields;
            }

            fields.TryGetValue(propertyKey, out var field);
            return field;
        }

        #endregion
    }
}