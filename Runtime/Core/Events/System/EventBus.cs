using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FluxFramework.Core
{
    /// <summary>
    /// Thread-safe event bus for decoupled communication
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<(Delegate handler, int priority)>> _subscribers = new();
        private readonly object _initLock = new object();
        private bool _isInitialized = false;

        private readonly IFluxThreadManager _threadManager;

        /// <summary>
        /// A global event that is fired whenever any event is published.
        /// This is primarily intended for debugging and monitoring tools.
        /// </summary>
        public event Action<IFluxEvent> OnEventPublished;

        public EventBus(IFluxThreadManager threadManager)
        {
            _threadManager = threadManager ?? throw new ArgumentNullException(nameof(threadManager));
        }

        /// <summary>
        /// Initializes the event bus
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            lock (_initLock)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                }
            }
        }

        /// <summary>
        /// Subscribes to events of type T and returns an IDisposable for easy unsubscription.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IFluxEvent
        {
            var eventType = typeof(T);
            var subscribers = _subscribers.GetOrAdd(eventType, _ => new ConcurrentBag<(Delegate, int)>());
            var subscriptionTuple = (handler as Delegate, priority);
            subscribers.Add(subscriptionTuple);

            return new EventSubscription<T>(this, handler);
        }

        /// <summary>
        /// Unsubscribes from events of type T.
        /// NOTE: This operation is heavier than Subscribe due to ConcurrentBag limitations.
        /// </summary>
        /// <typeparam name="T">The event type.</typeparam>
        /// <param name="handler">The event handler to remove.</param>
        public void Unsubscribe<T>(Action<T> handler) where T : IFluxEvent
        {
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var currentSubscribers))
            {
                return; // No subscribers for this event type, nothing to do.
            }

            // --- Thread-Safe Removal Logic ---
            var newSubscribers = new ConcurrentBag<(Delegate handler, int priority)>();

            // Create a list of the items we want to keep.
            // We cannot simply iterate and add to the new bag because another thread might be adding items
            // to the old bag at the same time. The correct approach is to atomically replace the bag.

            var subscribersToKeep = currentSubscribers.Where(sub => !sub.handler.Equals(handler)).ToList();
            foreach (var sub in subscribersToKeep)
            {
                newSubscribers.Add(sub);
            }

            // Atomically swap the old bag with the new one.
            // If another thread has modified the bag in the meantime, TryUpdate will fail, and we may need to retry.
            // For simplicity, a single attempt is often sufficient unless unsubscribes are extremely frequent and concurrent.
            if (!_subscribers.TryUpdate(eventType, newSubscribers, currentSubscribers))
            {
                // Optional: Implement a retry loop if high-contention is expected.
                // For most use cases, this is not necessary.
                UnityEngine.Debug.LogWarning($"[FluxFramework] EventBus Unsubscribe failed due to high contention for event type {eventType.Name}. The handler might not have been removed.");
            }
        }

        /// <summary>
        /// Unsubscribes a specific object from ALL event types it has subscribed to.
        /// </summary>
        /// <param name="target">The subscriber object to remove (e.g., 'this' from a MonoBehaviour).</param>
        public void Unsubscribe(object target)
        {
            if (target == null) return;

            // Iterate over a snapshot of the keys to avoid issues if the collection is modified.
            foreach (var eventType in _subscribers.Keys.ToList())
            {
                if (_subscribers.TryGetValue(eventType, out var currentSubscribers))
                {
                    // Create a new collection containing only the delegates that DO NOT belong to the target object.
                    // The Delegate.Target property gives us the instance the handler is bound to.
                    var newSubscribers = new ConcurrentBag<(Delegate handler, int priority)>(
                        currentSubscribers.Where(sub => sub.handler.Target != target)
                    );

                    // Atomically replace the old collection with the new one.
                    _subscribers.TryUpdate(eventType, newSubscribers, currentSubscribers);
                }
            }
        }

        /// <summary>
        /// Publishes an event to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventArgs">Event data</param>
        public void Publish<T>(T eventArgs) where T : IFluxEvent
        {
            // Fire the global monitoring event first.
            // We do this outside of the main thread execution to ensure the monitor
            // receives the event immediately, even if the handlers are delayed.
            try
            {
                OnEventPublished?.Invoke(eventArgs);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[FluxFramework] Error in a global event monitor: {e}");
            }

            var eventType = typeof(T);
            if (_subscribers.TryGetValue(eventType, out var subscribers))
            {
                // 1. Copy and sort subscribers by priority (descending order)
                var sortedHandlers = subscribers
                    .OfType<(Action<T> handler, int priority)>()
                    .OrderByDescending(sub => sub.priority)
                    .ToList();

                // 2. Execute on the main thread
                if (Flux.Manager != null)
                {
                    _threadManager.ExecuteOnMainThread(() =>
                    {
                        foreach (var sub in sortedHandlers)
                        {
                            try { sub.handler(eventArgs); }
                            catch (Exception e) { UnityEngine.Debug.LogError($"[FluxFramework] Error in event handler: {e}"); }
                        }
                    });
                }
                else
                {
                    foreach (var sub in sortedHandlers)
                    {
                        try
                        {
                            sub.handler(eventArgs);
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogError($"[FluxFramework] Error in event handler: {e}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears all subscribers
        /// </summary>
        public void Clear()
        {
            _subscribers.Clear();
        }

        /// <summary>
        /// Gets the number of subscribers for an event type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Number of subscribers</returns>
        public int GetSubscriberCount<T>() where T : IFluxEvent
        {
            var eventType = typeof(T);
            return _subscribers.TryGetValue(eventType, out var subscribers) ? subscribers.Count : 0;
        }

        /// <summary>
        /// Gets the total number of subscriptions across all event types.
        /// </summary>
        /// <returns>The total count of active event subscriptions.</returns>
        public int GetTotalSubscriberCount()
        {
            if (!_isInitialized) return 0;

            // Sums the count of subscribers for each event type.
            return _subscribers.Values.Sum(bag => bag.Count);
        }
        
        private sealed class EventSubscription<T> : IDisposable where T : IFluxEvent
        {
            private EventBus _owner;
            private readonly Action<T> _handler;
            public EventSubscription(EventBus owner, Action<T> handler) { _owner = owner; _handler = handler; }
            public void Dispose() { _owner?.Unsubscribe(_handler); _owner = null; }
        }
    }
}
