using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Utils;

namespace FluxFramework.Core
{
    /// <summary>
    /// Manages reactive properties for the Flux Framework
    /// </summary>
    public class FluxPropertyManager : IFluxPropertyManager
    {
        private class PropertyRegistration
        {
            public IReactiveProperty Property;
            public bool IsPersistent;
        }
        private readonly ConcurrentDictionary<string, PropertyRegistration> _properties = new();

        /// <summary>
        /// An event that is invoked whenever a new reactive property is registered with the manager.
        /// </summary>
        public event Action<string, IReactiveProperty> OnPropertyRegistered;

        /// <summary>
        /// Registers a reactive property with the framework
        /// </summary>
        /// <param name="key">Unique key for the property</param>
        /// <param name="property">Reactive property instance</param>
        /// <param name="isPersistent">Whether the property should persist across scene loads</param>
        public void RegisterProperty(string key, IReactiveProperty property, bool isPersistent)
        {
            var registration = new PropertyRegistration { Property = property, IsPersistent = isPersistent };
            if (_properties.TryAdd(key, registration))
            {
                OnPropertyRegistered?.Invoke(key, property);
            }
            else
            {
                _properties[key] = registration;
            }
        }

        /// <summary>
        /// Gets a reactive property by key
        /// </summary>
        /// <typeparam name="T">Type of the property value</typeparam>
        /// <param name="key">Property key</param>
        /// <returns>Reactive property or null if not found</returns>
        public ReactiveProperty<T> GetProperty<T>(string key)
        {
            return _properties.TryGetValue(key, out var registration) ? registration.Property as ReactiveProperty<T> : null;
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
            // We first check if a 'registration' exists.
            if (_properties.TryGetValue(key, out var registration) && registration.Property is ReactiveProperty<T> typedProperty)
            {
                // If the registration exists AND its 'Property' is of the correct type, return it.
                return typedProperty;
            }

            // If not found, create a new property.
            var newProperty = new ReactiveProperty<T>(defaultValue);
            
            // Register it as NON-PERSISTENT by default. This is a safe assumption for a property
            // that is being created on-the-fly.
            RegisterProperty(key, newProperty, false); 
            
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
            _properties.TryGetValue(key, out var registration);
            return registration?.Property;
        }

        /// <summary>
        /// Gets the key associated with a given reactive property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public string GetKey(IReactiveProperty property)
        {
            foreach (var kvp in _properties)
            {
                if (kvp.Value.Property == property)
                {
                    return kvp.Key;
                }
            }
            return null;
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
        /// Subscribes a handler to a property, either immediately if it exists,
        /// or later when it gets registered.
        /// </summary>
        /// <returns>An IDisposable that can be used to cancel the deferred subscription.</returns>
        public IDisposable SubscribeDeferred(string key, Action<IReactiveProperty> onSubscribe)
        {
            // Case 1: The property already exists. We get the 'registration' object.
            if (_properties.TryGetValue(key, out var registration))
            {
                // We then invoke the callback with the 'Property' member of the registration.
                onSubscribe(registration.Property);
                return new NoOpDisposable(); // The subscription is live, nothing to cancel here.
            }

            // Case 2: The property does not exist yet. Listen for its future creation.
            // This logic does not need to change, because the 'OnPropertyRegistered' event
            // still provides the IReactiveProperty directly, so the handler works as-is.
            Action<string, IReactiveProperty> registrationHandler = null;
            registrationHandler = (registeredKey, registeredProperty) =>
            {
                if (registeredKey == key)
                {
                    onSubscribe(registeredProperty);
                    OnPropertyRegistered -= registrationHandler;
                }
            };

            OnPropertyRegistered += registrationHandler;

            return new ActionDisposable(() => { OnPropertyRegistered -= registrationHandler; });
        }

        /// <summary>
        /// Gets the number of registered properties
        /// </summary>
        public int PropertyCount => _properties.Count;
        
        /// <summary>
        /// Clears all non-persistent properties (to be called on scene load)
        /// </summary>
        public void ClearNonPersistentProperties()
        {
            var keysToRemove = _properties
                .Where(kvp => !kvp.Value.IsPersistent)
                .Select(kvp => kvp.Key)
                .ToList();

            int clearedCount = 0;
            foreach (var key in keysToRemove)
            {
                if (_properties.TryRemove(key, out var registration))
                {
                    registration.Property.Dispose();
                    clearedCount++;
                }
            }
            
            if (clearedCount > 0)
            {
                Debug.Log($"[FluxFramework] Cleared {clearedCount} non-persistent properties on scene load.");
            }
        }

        /// <summary>
        /// Clears all properties
        /// </summary>
        public void Clear()
        {
            foreach (var registration in _properties.Values)
            {
                registration.Property.Dispose();
            }
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
        
        private class NoOpDisposable : IDisposable { public void Dispose() { } }
    }
}
