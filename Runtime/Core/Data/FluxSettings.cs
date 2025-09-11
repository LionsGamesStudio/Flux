using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base class for settings ScriptableObjects with automatic persistence and validation
    /// Integrates with the Flux Framework for reactive settings management
    /// </summary>
    public abstract class FluxSettings : FluxScriptableObject
    {
        [Header("Settings Configuration")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private bool validateOnLoad = true;
        [SerializeField] private string settingsKey = "";

        /// <summary>
        /// Event fired when settings are loaded
        /// </summary>
        public System.Action OnSettingsLoaded;
        
        /// <summary>
        /// Event fired when settings are saved
        /// </summary>
        public System.Action OnSettingsSaved;

        /// <summary>
        /// Event fired when a setting value changes
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

        protected sealed override void OnReactivePropertiesInitialized()
        {
            base.OnReactivePropertiesInitialized();
            
            // Subscribe to all property changes for auto-save
            if (autoSave)
            {
                SetupAutoSave();
            }
            
            // Call the overridable method for child classes
            OnSettingsInitialized();
        }
        
        /// <summary>
        /// Override this method for custom initialization logic after settings properties are set up
        /// </summary>
        protected virtual void OnSettingsInitialized()
        {
            // Child classes can override this
        }

        /// <summary>
        /// Sets up automatic saving when properties change
        /// </summary>
        private void SetupAutoSave()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var propertyKey = reactiveAttr.Key;
                    
                    if (field.FieldType == typeof(int))
                    {
                        SubscribeToProperty<int>(propertyKey, value => OnSettingChangedInternal(propertyKey, value));
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        SubscribeToProperty<float>(propertyKey, value => OnSettingChangedInternal(propertyKey, value));
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        SubscribeToProperty<bool>(propertyKey, value => OnSettingChangedInternal(propertyKey, value));
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        SubscribeToProperty<string>(propertyKey, value => OnSettingChangedInternal(propertyKey, value));
                    }
                }
            }
        }

        /// <summary>
        /// Called when a setting value changes
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
        /// Load settings from PlayerPrefs or file
        /// </summary>
        [ContextMenu("Load Settings")]
        public virtual void LoadSettings()
        {
            var key = string.IsNullOrEmpty(settingsKey) ? GetType().Name : settingsKey;
            
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var propertyKey = $"{key}.{field.Name}";
                    
                    if (field.FieldType == typeof(int))
                    {
                        var value = PlayerPrefs.GetInt(propertyKey, (int)field.GetValue(this));
                        UpdateReactiveProperty(reactiveAttr.Key, value);
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        var value = PlayerPrefs.GetFloat(propertyKey, (float)field.GetValue(this));
                        UpdateReactiveProperty(reactiveAttr.Key, value);
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        var value = PlayerPrefs.GetInt(propertyKey, (bool)field.GetValue(this) ? 1 : 0) == 1;
                        UpdateReactiveProperty(reactiveAttr.Key, value);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        var value = PlayerPrefs.GetString(propertyKey, (string)field.GetValue(this));
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
        /// Save settings to PlayerPrefs or file
        /// </summary>
        [ContextMenu("Save Settings")]
        public virtual void SaveSettings()
        {
            var key = string.IsNullOrEmpty(settingsKey) ? GetType().Name : settingsKey;
            
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var propertyKey = $"{key}.{field.Name}";
                    var fieldValue = field.GetValue(this);
                    
                    if (field.FieldType == typeof(int))
                    {
                        PlayerPrefs.SetInt(propertyKey, (int)fieldValue);
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        PlayerPrefs.SetFloat(propertyKey, (float)fieldValue);
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        PlayerPrefs.SetInt(propertyKey, (bool)fieldValue ? 1 : 0);
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        PlayerPrefs.SetString(propertyKey, (string)fieldValue);
                    }
                }
            }
            
            PlayerPrefs.Save();
            OnSettingsSaved?.Invoke();
        }

        /// <summary>
        /// Validate all settings using their validation attributes
        /// </summary>
        [ContextMenu("Validate Settings")]
        public virtual bool ValidateSettings()
        {
            bool allValid = true;
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    // Check validation attributes
                    var rangeAttr = field.GetCustomAttribute<FluxRangeAttribute>();
                    if (rangeAttr != null)
                    {
                        var value = field.GetValue(this);
                        if (field.FieldType == typeof(int))
                        {
                            var intValue = (int)value;
                            if (intValue < rangeAttr.Min || intValue > rangeAttr.Max)
                            {
                                Debug.LogWarning($"Setting {field.Name} value {intValue} is out of range [{rangeAttr.Min}, {rangeAttr.Max}]");
                                allValid = false;
                            }
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            var floatValue = (float)value;
                            if (floatValue < rangeAttr.Min || floatValue > rangeAttr.Max)
                            {
                                Debug.LogWarning($"Setting {field.Name} value {floatValue} is out of range [{rangeAttr.Min}, {rangeAttr.Max}]");
                                allValid = false;
                            }
                        }
                    }
                    
                    // Add more validation checks as needed
                }
            }
            
            return allValid;
        }

        /// <summary>
        /// Reset all settings to their default values
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
        /// Apply settings to the application (override in derived classes)
        /// </summary>
        public virtual void ApplySettings()
        {
            // Override in derived classes to apply settings to Unity systems
            // e.g., Screen.fullScreen, QualitySettings.SetQualityLevel, etc.
        }
    }
}
