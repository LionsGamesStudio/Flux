using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a method as an event handler for specific event types
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FluxEventHandlerAttribute : Attribute
    {
        /// <summary>
        /// Type of event this method handles
        /// </summary>
        public Type EventType { get; }

        /// <summary>
        /// Priority of this event handler (higher values execute first)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether this handler should run on the main thread
        /// </summary>
        public bool RequireMainThread { get; set; } = true;

        /// <summary>
        /// Whether this handler can run asynchronously
        /// </summary>
        public bool Async { get; set; } = false;

        public FluxEventHandlerAttribute(Type eventType)
        {
            EventType = eventType;
        }

        /// <summary>
        /// Marks a method as a Flux event handler. The event type will be inferred
        /// from the method's first parameter.
        /// </summary>
        /// <example>
        /// [FluxEventHandler]
        /// private void OnPlayerDied(PlayerDiedEvent evt) { ... }
        /// </example>
        public FluxEventHandlerAttribute()
        {
            EventType = null; // Mark for inference
        }
    }
}
