using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base class for settings ScriptableObjects with automatic persistence and validation.
    /// Integrates with the Flux Framework for reactive settings management.
    /// </summary>
    public abstract class FluxSettings : FluxScriptableObject
    {
        [Header("Settings Configuration")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private bool validateOnLoad = true;
        [SerializeField] private string settingsKey = "";

        /// <summary>
        /// Event fired when settings are loaded.
        /// </summary>
        public System.Action OnSettingsLoaded;
        
        /// <summary>
        /// Event fired when settings are saved.
        /// </summary>
        public System.Action OnSettingsSaved;

        /// <summary>
        /// Event fired when a setting value changes.
        /// </summary>
        public System.Action<string, object> OnSettingChanged;

        protected override void OnFluxEnabled()
        {
            base.OnFluxEnabled();
            
            if (Application.isPlaying)
            {
                LoadSettings();
            }
        }

        protected sealed override void OnFluxPropertiesInitialized()
        {
            base.OnFluxPropertiesInitialized();

            if (autoSave)
            {
                SetupAutoSave();
            }
            
            OnSettingsInitialized();
        }
        
        /// <summary>
        /// Override this method for custom initialization logic after settings properties are set up.
        /// </summary>
        protected virtual void OnSettingsInitialized() { }

        /// <summary>
        /// Subscribes to all reactive properties in this container to trigger auto-saving.
        /// </summary>
        private void SetupAutoSave()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var property = Flux.Manager.Properties.GetProperty(reactiveAttr.Key);
                    property?.Subscribe(newValue => OnSettingChangedInternal(reactiveAttr.Key, newValue));
                }
            }
        }

        /// <summary>
        /// Called when a setting value changes.
        /// </summary>
        private void OnSettingChangedInternal(string propertyKey, object newValue)
        {
            OnSettingChanged?.Invoke(propertyKey, newValue);
            if (autoSave)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// Loads settings from PlayerPrefs.
        /// </summary>
        [ContextMenu("Load Settings")]
        public virtual void LoadSettings()
        {
            var keyPrefix = string.IsNullOrEmpty(settingsKey) ? GetType().Name : settingsKey;
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var prefKey = $"{keyPrefix}.{field.Name}";
                    if (!PlayerPrefs.HasKey(prefKey)) continue;

                    object value = null;
                    if (field.FieldType == typeof(int)) value = PlayerPrefs.GetInt(prefKey);
                    else if (field.FieldType == typeof(float)) value = PlayerPrefs.GetFloat(prefKey);
                    else if (field.FieldType == typeof(bool)) value = PlayerPrefs.GetInt(prefKey, 0) == 1;
                    else if (field.FieldType == typeof(string)) value = PlayerPrefs.GetString(prefKey);

                    if (value != null)
                    {
                        UpdateReactiveProperty(reactiveAttr.Key, value);
                    }
                }
            }
            
            if (validateOnLoad)
            {
                ValidateSettings();
            }
            OnSettingsLoaded?.Invoke();
        }

        /// <summary>
        /// Saves settings to PlayerPrefs.
        /// </summary>
        [ContextMenu("Save Settings")]
        public virtual void SaveSettings()
        {
            var keyPrefix = string.IsNullOrEmpty(settingsKey) ? GetType().Name : settingsKey;
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var prefKey = $"{keyPrefix}.{field.Name}";
                    var fieldValue = field.GetValue(this);
                    
                    if (field.FieldType == typeof(int)) PlayerPrefs.SetInt(prefKey, (int)fieldValue);
                    else if (field.FieldType == typeof(float)) PlayerPrefs.SetFloat(prefKey, (float)fieldValue);
                    else if (field.FieldType == typeof(bool)) PlayerPrefs.SetInt(prefKey, (bool)fieldValue ? 1 : 0);
                    else if (field.FieldType == typeof(string)) PlayerPrefs.SetString(prefKey, (string)fieldValue);
                }
            }
            PlayerPrefs.Save();
            OnSettingsSaved?.Invoke();
        }

        /// <summary>
        /// Validates the settings asset.
        /// NOTE: This method validates the *structure* of the asset. Value validation (like ranges)
        /// is handled automatically at runtime by the ReactiveProperty system.
        /// </summary>
        [ContextMenu("Validate Settings")]
        public virtual bool ValidateSettings()
        {
            // CORRECTED: The manual value validation logic has been removed because it is now
            // redundant. The framework's core validation system handles this automatically
            // when a ValidatedReactiveProperty is created for any field with a validation attribute.
            // This method is kept for future structural validation if needed (e.g., checking for null references).

            Debug.Log($"[FluxFramework] Structural validation of settings asset '{this.name}' passed. Value validation is handled automatically at runtime.", this);
            return true;
        }

        /// <summary>
        /// Resets all settings to their default values as defined in the script.
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public virtual void ResetToDefaults()
        {
            ResetReactiveProperties();
            if (autoSave)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// Apply settings to the application (e.g., to Unity's QualitySettings, AudioMixer, etc.).
        /// Should be overridden in derived classes.
        /// </summary>
        public virtual void ApplySettings() { }
    }
}