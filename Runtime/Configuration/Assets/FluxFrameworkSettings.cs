using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.Utils;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Main framework configuration
    /// </summary>
    [FluxConfiguration("Framework", 
        DisplayName = "Framework Settings", 
        Description = "Core framework configuration and behavior settings",
        LoadPriority = 100,
        IsRequired = true,
        AutoCreate = true,
        DefaultMenuPath = "Flux/Framework Settings")]
    [CreateAssetMenu(fileName = "FluxFrameworkSettings", menuName = "Flux/Framework Settings")]
    public class FluxFrameworkSettings : FluxConfigurationAsset
    {
        [Header("Framework Settings")]
        [Tooltip("Enables detailed debug messages from the framework. Set to Info or higher for production builds.")]
        public LogLevel logLevel = LogLevel.Info;

        [Tooltip("Maximum number of main thread actions queued per frame")]
        [Range(1, 1000)]
        public int maxMainThreadActionsPerFrame = 100;

        [Tooltip("Enable automatic component registration")]
        public bool autoRegisterComponents = true;

        [Header("Threading")]
        [Tooltip("Enable thread-safe operations")]
        public bool enableThreadSafety = true;

        [Tooltip("Timeout for thread-safe operations (milliseconds)")]
        [Range(100, 10000)]
        public int threadTimeoutMs = 5000;

        [Header("Event System")]
        [Tooltip("Maximum number of event subscribers per event type")]
        [Range(10, 1000)]
        public int maxEventSubscribers = 100;

        [Tooltip("Enable event performance monitoring")]
        public bool enableEventProfiling = false;

        [Header("UI Binding")]
        [Tooltip("Enable automatic UI binding")]
        public bool enableAutoUIBinding = true;

        [Tooltip("Default format string for numeric bindings")]
        public string defaultNumericFormat = "{0:F2}";

        public override bool ValidateConfiguration()
        {
            if (maxMainThreadActionsPerFrame <= 0)
            {
                Debug.LogError("[FluxFramework] maxMainThreadActionsPerFrame must be greater than 0");
                return false;
            }

            if (threadTimeoutMs <= 0)
            {
                Debug.LogError("[FluxFramework] threadTimeoutMs must be greater than 0");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the settings from this asset to the live framework systems.
        /// This is where the configuration becomes active.
        /// </summary>
        public override void ApplyConfiguration(IFluxManager manager)
        {
            if (!ValidateConfiguration()) return;

            // 1. Apply Logging Configuration
            FluxLogger.CurrentLogLevel = this.logLevel;

            // Example: Add a file logger if a specific setting was enabled.
            // if (this.enableFileLogging) {
            //     FluxLogger.AddHandler(new FileLogHandler("path/to/my/log.txt"));
            // }

            // 2. Apply Threading Configuration
            // This assumes your FluxThreadManager is a singleton or has static settings.
            // Example:
            // if (Flux.Manager != null) {
            //     Flux.Manager.Threading.SetMaxActionsPerFrame(this.maxMainThreadActionsPerFrame);
            // }

            // 3. Apply other configurations...
            
            Debug.Log($"[FluxFramework] Core settings applied. Log level set to: {this.logLevel}.");
        }
    }
}
