using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Extensions;
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
            Flux.OnFrameworkInitialized -= FrameworkInitializeOnce;

            if (_isFrameworkInitialized)
            {
                OnFluxDestroy();
            }
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
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
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
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
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
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T> onChanged, bool fireOnSubscribe = false)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T, T> onChanged, bool fireOnSubscribe = false)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        protected void PublishEvent<T>(T eventData) where T : IFluxEvent
        {
            Flux.Manager.EventBus.Publish(eventData);
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
        
        #region Collection Helper Methods

        /// <summary>
        /// Adds an item to a reactive collection identified by its key.
        /// </summary>
        /// <typeparam name="T">The type of item in the collection.</typeparam>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        /// <param name="itemToAdd">The item to add.</param>
        protected void AddToReactiveCollection<T>(string propertyKey, T itemToAdd)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveCollection<T> collection)
            {
                collection.Add(itemToAdd);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to add an item to '{propertyKey}', but it is not a valid ReactiveCollection<{typeof(T).Name}>.", this);
            }
        }

        /// <summary>
        /// Removes an item from a reactive collection identified by its key.
        /// </summary>
        /// <typeparam name="T">The type of item in the collection.</typeparam>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        /// <param name="itemToRemove">The item to remove.</param>
        /// <returns>True if the item was successfully removed.</returns>
        protected bool RemoveFromReactiveCollection<T>(string propertyKey, T itemToRemove)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);
            if (property is ReactiveCollection<T> collection)
            {
                return collection.Remove(itemToRemove);
            }
            
            return false;
        }

        /// <summary>
        /// Clears all items from a reactive collection identified by its key.
        /// </summary>
        /// <typeparam name="T">The type of item in the collection.</typeparam>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        protected void ClearReactiveCollection<T>(string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);
            if (property is ReactiveCollection<T> collection)
            {
                collection.Clear();
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to clear '{propertyKey}', but it is not a valid ReactiveCollection<{typeof(T).Name}>.", this);
            }
        }

        #endregion
        
        #region Dictionary Helper Methods

        /// <summary>
        /// Adds or updates a key-value pair in a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="value">The dictionary value.</param>
        protected void SetInReactiveDictionary<TKey, TValue>(string propertyKey, TKey key, TValue value)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                dictionary[key] = value;
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to set value in '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
            }
        }

        /// <summary>
        /// Adds a key-value pair to a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key.</param>
        /// <param name="value">The dictionary value.</param>
        /// <returns>True if the key-value pair was successfully added.</returns>
        protected bool AddToReactiveDictionary<TKey, TValue>(string propertyKey, TKey key, TValue value)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                try
                {
                    dictionary.Add(key, value);
                    return true;
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning($"[FluxFramework] Key '{key}' already exists in ReactiveDictionary '{propertyKey}'.", this);
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to add to '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
                return false;
            }
        }

        /// <summary>
        /// Removes a key-value pair from a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to remove.</param>
        /// <returns>True if the key-value pair was successfully removed.</returns>
        protected bool RemoveFromReactiveDictionary<TKey, TValue>(string propertyKey, TKey key)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.Remove(key);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to remove from '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
                return false;
            }
        }

        /// <summary>
        /// Gets a value from a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to get.</param>
        /// <param name="value">The retrieved value, if found.</param>
        /// <returns>True if the key was found and the value was retrieved.</returns>
        protected bool TryGetFromReactiveDictionary<TKey, TValue>(string propertyKey, TKey key, out TValue value)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.TryGetValue(key, out value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to get from '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Checks if a key exists in a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to check.</param>
        /// <returns>True if the key exists in the dictionary.</returns>
        protected bool ContainsKeyInReactiveDictionary<TKey, TValue>(string propertyKey, TKey key)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.ContainsKey(key);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to check key in '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
                return false;
            }
        }

        /// <summary>
        /// Clears all items from a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        protected void ClearReactiveDictionary<TKey, TValue>(string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                dictionary.Clear();
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to clear '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", this);
            }
        }

        #endregion
    }
}