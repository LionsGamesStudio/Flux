using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for reactive properties that can be observed for changes
    /// </summary>
    public interface IReactiveProperty
    {
        /// <summary>
        /// Type of the property value
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Gets the current value as object
        /// </summary>
        object GetValue();

        /// <summary>
        /// Sets the value from an object (with type checking)
        /// </summary>
        /// <param name="value">New value to set</param>
        /// <param name="forceNotify">Whether to force notification even if value hasn't changed</param>
        void SetValue(object value, bool forceNotify = false);

        /// <summary>
        /// Whether this property has any subscribers
        /// </summary>
        bool HasSubscribers { get; }

        /// <summary>
        /// Number of current subscribers
        /// </summary>
        int SubscriberCount { get; }

        /// <summary>
        /// Subscribes to value changes with a non-generic callback.
        /// </summary>
        /// <param name="onValueChanged">The callback to invoke when the value changes.</param>
        /// <returns>An IDisposable object to manage the subscription's lifecycle.</returns>
        IDisposable Subscribe(Action<object> onValueChanged);

        /// <summary>
        /// Disposes all subscriptions and cleans up resources
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Generic interface for typed reactive properties
    /// </summary>
    /// <typeparam name="T">Type of the property value</typeparam>
    public interface IReactiveProperty<T> : IReactiveProperty
    {
        /// <summary>
        /// Current value of the property
        /// </summary>
        T Value { get; set; }

        /// <summary>
        /// Subscribe to value changes
        /// </summary>
        /// <param name="onValueChanged">Callback for value changes</param>
        /// <returns>Disposable subscription</returns>
        IDisposable Subscribe(Action<T> onValueChanged);

        /// <summary>
        /// Subscribe to value changes with previous value
        /// </summary>
        /// <param name="onValueChanged">Callback with old and new values</param>
        /// <returns>Disposable subscription</returns>
        IDisposable Subscribe(Action<T, T> onValueChanged);
    }
}
