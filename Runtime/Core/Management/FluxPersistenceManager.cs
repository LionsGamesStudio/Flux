using UnityEngine;
using System.Collections.Generic;
using FluxFramework.Core;
using FluxFramework.Utils;
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

            LoadProperty(key, property);
            
            var subscription = property.Subscribe(value => SaveProperty(key, value), fireOnSubscribe: false);
            _persistentSubscriptions[key] = subscription;

            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Registered '{key}' for persistence.");
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
            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Loaded all persistent properties.");
        }
        
        /// <summary>
        /// Saves all pending changes to disk. Should be called when the application quits or pauses.
        /// </summary>
        public void SaveAll()
        {
            PlayerPrefs.Save();
            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] All persistent properties saved to disk.");
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
                return;
            }

            string jsonValue = PlayerPrefs.GetString(playerPrefsKey);

            try
            {
                object value = FluxJsonUtils.Deserialize(jsonValue, property.ValueType);
                
                if (value != null)
                {
                    property.SetValue(value, forceNotify: false);
                }
            }
            catch (Exception ex)
            {
                FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Failed to load/deserialize persistent property '{key}': {ex.Message}");
            }
        }

        private void SaveProperty(string key, object value)
        {
            if (value == null) return;
            
            string playerPrefsKey = PLAYER_PREFS_PREFIX + key;

            try
            {
                string jsonValue = FluxJsonUtils.Serialize(value);
                PlayerPrefs.SetString(playerPrefsKey, jsonValue);
            }
            catch (Exception ex)
            {
                FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Failed to save/serialize persistent property '{key}': {ex.Message}");
            }
        }
    }
}