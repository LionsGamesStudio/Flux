using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base abstract class for Flux events with common properties
    /// </summary>
    public abstract class FluxEventBase : IFluxEvent
    {
        public DateTime Timestamp { get; private set; }
        public string EventId { get; private set; }
        public string Source { get; protected set; }

        protected FluxEventBase(string source = null)
        {
            Timestamp = DateTime.UtcNow;
            EventId = Guid.NewGuid().ToString();
            Source = source ?? GetType().Name;
        }
    }
}
