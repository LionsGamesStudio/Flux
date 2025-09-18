using UnityEngine;
using System.Collections.Generic;
using FluxFramework.Core;
using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Manages the persistence of ReactiveProperties marked with the Persistent flag.
    /// Uses PlayerPrefs with JSON serialization for robust type handling.
    /// </summary>
    public class FluxPersistenceManager : IFluxPersistenceManager
    {
        private const string PLAYER_PREFS_PREFIX = "flux_persistent_";
        private readonly Dictionary<string, IDisposable> _persistentSubscriptions = new Dictionary<string, IDisposable>();
        private readonly IFluxPropertyManager _propertyManager;

        public FluxPersistenceManager(IFluxPropertyManager propertyManager)
        {
            _propertyManager = propertyManager;
        }


        /// <summary>
        /// Registers a reactive property to be managed by the persistence system.
        /// It loads the saved value (if any) and subscribes to its changes for auto-saving.
        /// </summary>
        /// <param name="key">The unique key of the property.</param>
        /// <param name="property">The reactive property instance.</param>
        public void RegisterPersistentProperty(string key, IReactiveProperty property)
        {
            if (property == null || _persistentSubscriptions.ContainsKey(key))
            {
                return;
            }

            // 1. Load the saved value for this property
            LoadProperty(key, property);

            // 2. Subscribe to changes to auto-save the value
            // We need a non-generic way to subscribe. IReactiveProperty does not have it,
            // so we must cast to the concrete implementation that has the non-generic event.
            if (property is IReactiveProperty a) // Need to define an interface with a generic subscribe
            {
                // This is a placeholder. To make this work, ReactiveProperty must expose
                // a way to subscribe non-generically or the interface must define it.
                // Let's assume we add `IDisposable Subscribe(Action<object> callback)` to `IReactiveProperty`.

                // --- LET'S REFACTOR IReactiveProperty INTERFACE FIRST ---
                // For this to work, we'll assume the IReactiveProperty interface is updated like this:
                /*
                public interface IReactiveProperty {
                    // ... existing members
                    IDisposable Subscribe(Action<object> onValueChanged);
                }
                
                // And ReactiveProperty<T> implements it:
                public IDisposable Subscribe(Action<object> callback)
                {
                    // ... (Implementation from previous task)
                }
                */

                var subscription = a.Subscribe(value => SaveProperty(key, value));
                _persistentSubscriptions[key] = subscription;

                Debug.Log($"[FluxFramework] Registered '{key}' for persistence.");
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Property with key '{key}' could not be registered for persistence as it does not support non-generic subscription.");
            }
        }

        /// <summary>
        /// Loads all properties that have been registered as persistent.
        /// NOTE: This is generally not needed if registration happens at runtime.
        /// This could be used if keys were pre-registered.
        /// </summary>
        public void LoadAllRegisteredProperties()
        {
            foreach (var key in _persistentSubscriptions.Keys)
            {
                var property = _propertyManager.GetProperty(key);
                if (property != null)
                {
                    LoadProperty(key, property);
                }
            }
            Debug.Log($"[FluxFramework] Loaded all persistent properties.");
        }
        
        /// <summary>
        /// Saves all pending changes to disk. Should be called when the application quits or pauses.
        /// </summary>
        public void SaveAll()
        {
            PlayerPrefs.Save();
            Debug.Log("[FluxFramework] All persistent properties saved to disk.");
        }
        
        /// <summary>
        /// Clears all subscriptions and stops the persistence service.
        /// </summary>
        public void Shutdown()
        {
            foreach (var subscription in _persistentSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _persistentSubscriptions.Clear();
        }

        private void LoadProperty(string key, IReactiveProperty property)
        {
            string playerPrefsKey = PLAYER_PREFS_PREFIX + key;
            if (!PlayerPrefs.HasKey(playerPrefsKey))
            {
                return; // No saved value for this key
            }

            string jsonValue = PlayerPrefs.GetString(playerPrefsKey);

            try
            {
                // JsonUtility can only deserialize to objects, not primitives directly.
                // We wrap the value in a simple container object for serialization.
                var wrapperType = typeof(JsonValueWrapper<>).MakeGenericType(property.ValueType);
                object wrapper = JsonUtility.FromJson(jsonValue, wrapperType);
                object value = wrapper.GetType().GetField("value").GetValue(wrapper);
                
                // Set the value without forcing a notification, to avoid an immediate re-save.
                property.SetValue(value, forceNotify: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Failed to load/deserialize persistent property '{key}': {ex.Message}");
            }
        }

        private void SaveProperty(string key, object value)
        {
            if (value == null) return;
            
            string playerPrefsKey = PLAYER_PREFS_PREFIX + key;

            try
            {
                // Wrap the value for robust JSON serialization
                var wrapperType = typeof(JsonValueWrapper<>).MakeGenericType(value.GetType());
                object wrapper = Activator.CreateInstance(wrapperType);
                wrapper.GetType().GetField("value").SetValue(wrapper, value);
                
                string jsonValue = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(playerPrefsKey, jsonValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Failed to save/serialize persistent property '{key}': {ex.Message}");
            }
        }

        // Helper class to allow JsonUtility to serialize primitive types and structs.
        [Serializable]
        private class JsonValueWrapper<T>
        {
            public T value;
        }
    }
}