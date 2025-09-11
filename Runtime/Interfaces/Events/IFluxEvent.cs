using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base interface for all Flux events
    /// Events are used to communicate between different parts of the application
    /// </summary>
    public interface IFluxEvent
    {
        /// <summary>
        /// Timestamp when the event was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Unique identifier for this event instance
        /// </summary>
        string EventId { get; }

        /// <summary>
        /// Optional source component or system that triggered this event
        /// </summary>
        string Source { get; }
    }
}
