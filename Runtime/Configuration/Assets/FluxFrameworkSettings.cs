using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.Utils;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// The main configuration asset for the Flux Framework.
    /// Defines core behaviors, logging levels, and other foundational settings.
    /// </summary>
    [FluxConfiguration("Framework", 
        DisplayName = "Framework Settings", 
        Description = "Core framework configuration and behavior settings",
        LoadPriority = 100, // Highest priority to ensure it runs first
        IsRequired = true,
        AutoCreate = true,
        DefaultMenuPath = "Flux/Framework Settings")]
    [CreateAssetMenu(fileName = "FluxFrameworkSettings", menuName = "Flux/Framework Settings")]
    public class FluxFrameworkSettings : FluxConfigurationAsset
    {
        [Header("Framework Settings")]
        [Tooltip("The minimum level of messages to log. Set to Info or higher for production builds.")]
        public LogLevel logLevel = LogLevel.Info;

        [Tooltip("Maximum number of main thread actions to process in a single frame.")]
        [Range(1, 1000)]
        public int maxMainThreadActionsPerFrame = 100;

        [Tooltip("Enables the automatic discovery and registration of components marked with [FluxComponent].")]
        public bool autoRegisterComponents = true;

        [Header("Threading")]
        [Tooltip("Enables thread-safe mechanisms for framework operations.")]
        public bool enableThreadSafety = true;

        [Tooltip("Default timeout for thread-safe operations in milliseconds.")]
        [Range(100, 10000)]
        public int threadTimeoutMs = 5000;

        [Header("Event System")]
        [Tooltip("A soft limit for the number of subscribers per event type to detect potential memory leaks.")]
        [Range(10, 1000)]
        public int maxEventSubscribers = 100;

        [Tooltip("Enables performance monitoring hooks for the event system.")]
        public bool enableEventProfiling = false;

        [Header("UI Binding")]
        [Tooltip("Enables the automatic discovery and creation of UI bindings.")]
        public bool enableAutoUIBinding = true;

        [Tooltip("The default string format used for binding numbers to text components (e.g., '{0:F2}' for two decimal places).")]
        public string defaultNumericFormat = "{0:F2}";

        /// <summary>
        /// Validates the configuration values to ensure they are within acceptable ranges.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public override bool ValidateConfiguration()
        {
            if (maxMainThreadActionsPerFrame <= 0)
            {
                // NOTE: Using Debug.LogError here is acceptable because if validation fails,
                // we want to ensure the message is visible even if the logger isn't fully configured yet.
                Debug.LogError("[FluxFramework] 'maxMainThreadActionsPerFrame' must be greater than 0 in FluxFrameworkSettings.", this);
                return false;
            }

            if (threadTimeoutMs <= 0)
            {
                Debug.LogError("[FluxFramework] 'threadTimeoutMs' must be greater than 0 in FluxFrameworkSettings.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the settings from this asset to the live framework systems.
        /// This is where the configuration becomes active.
        /// </summary>
        /// <param name="manager">The IFluxManager instance to configure.</param>
        public override void ApplyConfiguration(IFluxManager manager)
        {
            if (!ValidateConfiguration()) return;

            // --- 1. Apply Logging Configuration ---
            manager.Logger.CurrentLogLevel = this.logLevel;

            // Example: The pattern for adding a handler is now also through the manager.
            // if (this.enableFileLogging) {
            //     manager.Logger.AddHandler(new FileLogHandler("path/to/my/log.txt"));
            // }

            // --- 2. Apply Threading Configuration ---
            manager.Threading.SetMaxActionsPerFrame(this.maxMainThreadActionsPerFrame);

            // --- 3. Apply other configurations... ---
            
            // CHANGED: Using the manager's logger instance for consistency.
            // Also removed the "[FluxFramework]" prefix from the message, as the logger adds it automatically.
            manager.Logger.Info($"Core settings applied. Log level set to: {this.logLevel}.", this);
        }
    }
}