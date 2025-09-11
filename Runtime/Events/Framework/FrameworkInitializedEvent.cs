using System;

namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when the framework is successfully initialized
    /// </summary>
    public class FrameworkInitializedEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Version of the framework that was initialized
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Whether the initialization was successful
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Time taken for initialization in milliseconds
        /// </summary>
        public long InitializationTimeMs { get; }

        public FrameworkInitializedEvent(string version, bool success, long initTimeMs = 0) 
            : base("FluxFramework.Core")
        {
            Version = version;
            Success = success;
            InitializationTimeMs = initTimeMs;
        }
    }
}
