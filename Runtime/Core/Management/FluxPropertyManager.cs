using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FluxFramework.Core
{
    /// <summary>
    /// Manages reactive properties for the Flux Framework
    /// </summary>
    public class FluxPropertyManager
    {
        private readonly ConcurrentDictionary<string, IReactiveProperty> _properties = new();

        /// <summary>
        /// Registers a reactive property with the framework
        /// </summary>
        /// <param name="key">Unique key for the property</param>
        /// <param name="property">Reactive property instance</param>
        public void RegisterProperty(string key, IReactiveProperty property)
        {
            _properties.TryAdd(key, property);
        }

        /// <summary>
        /// Gets a reactive property by key
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="key">Property key</param>
        /// <returns>Reactive property or null if not found</returns>
        public ReactiveProperty<T> GetProperty<T>(string key)
        {
            return _properties.TryGetValue(key, out var property) ? property as ReactiveProperty<T> : null;
        }

        /// <summary>
        /// Gets or creates a reactive property
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="key">Property key</param>
        /// <param name="defaultValue">Default value if property doesn't exist</param>
        /// <returns>Reactive property</returns>
        public ReactiveProperty<T> GetOrCreateProperty<T>(string key, T defaultValue = default)
        {
            if (_properties.TryGetValue(key, out var existing) && existing is ReactiveProperty<T> typedProperty)
            {
                return typedProperty;
            }

            var newProperty = new ReactiveProperty<T>(defaultValue);
            RegisterProperty(key, newProperty);
            return newProperty;
        }

        /// <summary>
        /// Removes a property from the manager
        /// </summary>
        /// <param name="key">Property key to remove</param>
        /// <returns>True if property was removed</returns>
        public bool RemoveProperty(string key)
        {
            return _properties.TryRemove(key, out _);
        }

        /// <summary>
        /// Checks if a property exists
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property exists</returns>
        public bool HasProperty(string key)
        {
            return _properties.ContainsKey(key);
        }

        /// <summary>
        /// Gets a reactive property by key (non-generic version)
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Reactive property or null if not found</returns>
        public IReactiveProperty GetProperty(string key)
        {
            _properties.TryGetValue(key, out var property);
            return property;
        }

        /// <summary>
        /// Unregisters a property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>True if property was removed</returns>
        public bool UnregisterProperty(string key)
        {
            return _properties.TryRemove(key, out _);
        }

        /// <summary>
        /// Gets all property keys
        /// </summary>
        /// <returns>Collection of property keys</returns>
        public IEnumerable<string> GetAllPropertyKeys()
        {
            return _properties.Keys;
        }

        /// <summary>
        /// Gets the number of registered properties
        /// </summary>
        public int PropertyCount => _properties.Count;

        /// <summary>
        /// Clears all properties
        /// </summary>
        public void Clear()
        {
            _properties.Clear();
        }

        /// <summary>
        /// Gets all property keys
        /// </summary>
        /// <returns>Array of property keys</returns>
        public string[] GetAllKeys()
        {
            var keys = new string[_properties.Count];
            _properties.Keys.CopyTo(keys, 0);
            return keys;
        }
    }
}
