using System;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// Provides extension methods for any object that implements IFluxReactiveObject,
    /// centralizing the logic for interacting with reactive properties.
    /// </summary>
    public static class FluxPropertyOwnerExtensions
    {
        // The field cache is moved here from FluxMonoBehaviour/FluxDataContainer
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<string, FieldInfo>> _fieldCache =
            new System.Collections.Concurrent.ConcurrentDictionary<Type, Dictionary<string, FieldInfo>>();

        #region Helper Methods

        /// <summary>
        /// Updates the value of a reactive property directly.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="propertyKey">The key of the reactive property</param>
        /// <param name="newValue">The new value to set</param>
        public static void UpdateReactiveProperty<T>(this IFluxReactiveObject owner, string propertyKey, T newValue)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = newValue;
                owner.ForceSyncLocalField(propertyKey, property.Value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to update property '{propertyKey}', but it does not exist.");
            }
        }

        /// <summary>
        /// Updates the value of a reactive property using a provided update function.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="propertyKey">The key of the reactive property</param>
        /// <param name="updateFunction">A function that takes the current value and returns the updated value</param>
        public static void UpdateReactiveProperty<T>(this IFluxReactiveObject owner, string propertyKey, Func<T, T> updateFunction)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = updateFunction(property.Value);
                owner.ForceSyncLocalField(propertyKey, property.Value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to update property '{propertyKey}', but it does not exist.");
            }
        }

        /// <summary>
        /// Updates the value of a reactive property directly.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="property">The reactive property to update</param>
        /// <param name="newValue">The new value to set</param>
        public static void UpdateReactiveProperty<T>(this IFluxReactiveObject owner, ReactiveProperty<T> property, T newValue)
        {
            if (property != null)
            {
                property.Value = newValue;
            }
        }

        /// <summary>
        /// Gets the current value of a reactive property.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="propertyKey">The key of the reactive property</param>
        /// <returns>The current value of the property, or default(T) if not found</
        public static T GetReactivePropertyValue<T>(this IFluxReactiveObject owner, string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        /// <summary>
        /// Subscribes to changes in a reactive property.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="propertyKey">The key of the reactive property</param>
        /// <param name="onChanged">Action to invoke when the property changes</param>
        /// <param name="fireOnSubscribe">Whether to invoke the action immediately with the current value</param>
        /// <returns>An IDisposable to unsubscribe from the property changes</returns>
        public static IDisposable SubscribeToProperty<T>(this IFluxReactiveObject owner, string propertyKey, Action<T> onChanged, bool fireOnSubscribe = false)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        /// <summary>
        /// Subscribes to changes in a reactive property.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="owner">The owner of the reactive property</param>
        /// <param name="propertyKey">The key of the reactive property</param>
        /// <param name="onChanged">Action to invoke when the property changes, with old and new values</param>
        /// <param name="fireOnSubscribe">Whether to invoke the action immediately with the current value</param>
        /// <returns>An IDisposable to unsubscribe from the property changes</returns>
        public static IDisposable SubscribeToProperty<T>(this IFluxReactiveObject owner, string propertyKey, Action<T, T> onChanged, bool fireOnSubscribe = false)
        {
            var property = Flux.Manager.Properties.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        /// <summary>
        /// Publishes an event to the Flux event bus.
        /// </summary>
        /// <typeparam name="T">Type of the event</typeparam>
        /// <param name="owner">The owner of the event</param>
        /// <param name="eventData">The event data to publish</param>
        public static void PublishEvent<T>(this IFluxReactiveObject owner, T eventData) where T : IFluxEvent
        {
            Flux.Manager.EventBus.Publish(eventData);
        }
        
        /// <summary>
        /// Ensures that the local field corresponding to the reactive property is synchronized with the property's value.
        /// </summary>
        /// <param name="owner">The owner of the reactive property.</param>
        /// <param name="propertyKey">The key of the reactive property.</param>
        /// <param name="managerValue">The current value from the Flux Manager.</param>
        private static void ForceSyncLocalField(this IFluxReactiveObject owner, string propertyKey, object managerValue)
        {
            var field = owner.GetFieldForPropertyKey(propertyKey);
            if (field != null)
            {
                field.SetValue(owner, managerValue);
            }
        }

        /// <summary>
        /// Gets the FieldInfo for a given property key, using a cache to optimize repeated lookups.
        /// </summary>
        /// <param name="owner">The owner of the reactive property.</param>
        /// <param name="propertyKey">The key of the reactive property.</param>
        /// <returns>The FieldInfo if found; otherwise, null.</returns>
        private static FieldInfo GetFieldForPropertyKey(this IFluxReactiveObject owner, string propertyKey)
        {
            var type = owner.GetType();
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                fields = new Dictionary<string, FieldInfo>();
                var allFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="owner">The owner of the reactive collection.</param>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        /// <param name="itemToAdd">The item to add to the collection.</param>
        public static void AddToReactiveCollection<T>(this IFluxReactiveObject owner, string propertyKey, T itemToAdd)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);
            if (property is ReactiveCollection<T> collection)
            {
                collection.Add(itemToAdd);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to add an item to '{propertyKey}', but it is not a valid ReactiveCollection<{typeof(T).Name}>.");
            }
        }

        /// <summary>
        /// Removes an item from a reactive collection identified by its key.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="owner">The owner of the reactive collection.</param>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        /// <param name="itemToRemove">The item to remove from the collection.</param>
        /// <returns>True if the item was successfully removed.</returns>
        public static bool RemoveFromReactiveCollection<T>(this IFluxReactiveObject owner, string propertyKey, T itemToRemove)
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
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="owner">The owner of the reactive collection.</param>
        /// <param name="propertyKey">The key of the reactive collection.</param>
        public static void ClearReactiveCollection<T>(this IFluxReactiveObject owner, string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);
            if (property is ReactiveCollection<T> collection)
            {
                collection.Clear();
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to clear '{propertyKey}', but it is not a valid ReactiveCollection<{typeof(T).Name}>.");
            }
        }

        #endregion

        #region Dictionary Helper Methods

        /// <summary>
        /// Sets a value in a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="owner">The owner of the reactive dictionary.</param>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to set.</param>
        /// <param name="value">The value to set for the specified key.</param>
        public static void SetInReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner, string propertyKey, TKey key, TValue value)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);
            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                dictionary[key] = value;
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to set value in '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.");
            }
        }

        /// <summary>
        /// Adds a key-value pair to a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="owner">The owner of the reactive dictionary.</param>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to add.</param>
        /// <param name="value">The value to add for the specified key.</param>
        public static bool AddToReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner, string propertyKey, TKey key, TValue value)
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
                    Debug.LogWarning($"[FluxFramework] Key '{key}' already exists in ReactiveDictionary '{propertyKey}'.");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to add to '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.");
                return false;
            }
        }
        
        /// <summary>
        /// Removes a key-value pair from a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="owner">The owner of the reactive dictionary.</param>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to remove.</param>
        /// <returns>True if the key-value pair was successfully removed.</returns>
        public static bool RemoveFromReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner,string propertyKey, TKey key)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.Remove(key);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to remove from '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", owner as UnityEngine.Object);
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
        public static bool TryGetFromReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner, string propertyKey, TKey key, out TValue value)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.TryGetValue(key, out value);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to get from '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", owner as UnityEngine.Object);
                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Checks if a key exists in a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="owner">The owner of the reactive dictionary.</param>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        /// <param name="key">The dictionary key to check.</param>
        /// <returns>True if the key exists in the dictionary.</returns>
        public static bool ContainsKeyInReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner, string propertyKey, TKey key)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                return dictionary.ContainsKey(key);
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to check key in '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", owner as UnityEngine.Object);
                return false;
            }
        }

        /// <summary>
        /// Clears all items from a reactive dictionary identified by its key.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="owner">The owner of the reactive dictionary.</param>
        /// <param name="propertyKey">The key of the reactive dictionary.</param>
        public static void ClearReactiveDictionary<TKey, TValue>(this IFluxReactiveObject owner, string propertyKey)
        {
            var property = Flux.Manager.Properties.GetProperty(propertyKey);

            if (property is ReactiveDictionary<TKey, TValue> dictionary)
            {
                dictionary.Clear();
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to clear '{propertyKey}', but it is not a valid ReactiveDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>.", owner as UnityEngine.Object);
            }
        }

        #endregion
    }
}