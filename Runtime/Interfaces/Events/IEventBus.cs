using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Defines the contract for the event bus service.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// A global event that is fired whenever any event is published.
        /// This is primarily intended for debugging and monitoring tools.
        /// </summary>
        event Action<IFluxEvent> OnEventPublished;

        /// <summary>
        /// Initializes the event bus
        /// </summary>
        void Initialize();

        /// <summary>
        /// Subscribes to events of type T and returns an IDisposable for easy unsubscription.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        IDisposable Subscribe<T>(Action<T> handler, int priority = 0) where T : IFluxEvent;

        /// <summary>
        /// Unsubscribes from events of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        void Unsubscribe<T>(Action<T> handler) where T : IFluxEvent;

        /// <summary>
        /// Unsubscribes all event handlers associated with the given target object.
        /// </summary>
        /// <param name="target"></param>
        void Unsubscribe(object target);

        /// <summary>
        /// Publishes an event to all subscribers of the event type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventArgs"></param>
        void Publish<T>(T eventArgs) where T : IFluxEvent;

        /// <summary>
        /// Clears all subscribers from the event bus.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the number of subscribers for the specified event type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        int GetSubscriberCount<T>() where T : IFluxEvent;

        /// <summary>
        /// Gets the total number of subscribers across all event types.
        /// </summary>
        /// <returns></returns>
        int GetTotalSubscriberCount();
    }
}