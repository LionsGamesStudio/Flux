using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.Extensions;

namespace FluxFramework.Core
{
    public abstract class FluxSettings : FluxScriptableObject
    {
        [Header("Settings Configuration")]
        [SerializeField] private bool validateOnLoad = true;
        [SerializeField] private string settingsKey = "";

        public System.Action OnSettingsLoaded;
        public System.Action OnSettingsSaved;
        public System.Action<string, object> OnSettingChanged;

        protected override void OnFluxEnabled()
        {
            base.OnFluxEnabled();
        }

        protected sealed override void OnFluxPropertiesInitialized()
        {
            base.OnFluxPropertiesInitialized();

            if (validateOnLoad)
            {
                ValidateSettings();
            }
            ApplySettings(); 
            OnSettingsLoaded?.Invoke();
            OnSettingsInitialized();
        }
        
        protected virtual void OnSettingsInitialized() { }

        [ContextMenu("Validate Settings")]
        public virtual bool ValidateSettings()
        {
            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Structural validation of settings asset '{this.name}' passed. Value validation is handled automatically at runtime.", this);
            return true;
        }

        [ContextMenu("Reset to Defaults")]
        public virtual void ResetToDefaults()
        {
            base.ResetReactiveProperties();
        }

        /// <summary>
        /// Apply the current settings to the relevant systems.
        /// This method should be overridden in derived classes to implement specific application logic.
        /// </summary>
        public abstract void ApplySettings();
    }
}