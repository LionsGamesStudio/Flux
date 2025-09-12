using System;
using System.Collections.Generic;
using FluxFramework.Core;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// Computed reactive property that derives its value from other properties
    /// </summary>
    /// <typeparam name="T">Type of the computed value</typeparam>
    public class ComputedProperty<T> : IReactiveProperty
    {
        private readonly Func<T> _computation;
        private T _cachedValue;
        private bool _isDirty = true;
        private readonly List<Action<T>> _subscribers = new List<Action<T>>();
        private readonly List<Action<object>> _objectSubscribers = new List<Action<object>>();
        private readonly object _lock = new object(); // Added for thread safety

        public Type ValueType => typeof(T);

        /// <summary>
        /// Gets whether this property has any subscribers
        /// </summary>
        public bool HasSubscribers
        {
            get { lock (_lock) { return _subscribers.Count > 0 || _objectSubscribers.Count > 0; } }
        }

        /// <summary>
        /// Gets the number of subscribers
        /// </summary>
        public int SubscriberCount
        {
            get
            {
                lock (_lock)
                {
                    return _subscribers.Count + _objectSubscribers.Count;
                }
            }
        }

        /// <summary>
        /// Current computed value
        /// </summary>
        public T Value
        {
            get
            {
                if (_isDirty)
                {
                    RecomputeValue();
                }
                return _cachedValue;
            }
        }

        public ComputedProperty(Func<T> computation)
        {
            _computation = computation ?? throw new ArgumentNullException(nameof(computation));
        }

        /// <summary>
        /// Marks the computed property as dirty, forcing recomputation on next access
        /// </summary>
        public void Invalidate()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Forces immediate recomputation of the value
        /// </summary>
        /// <returns>The newly computed value</returns>
        public T Recompute()
        {
            _isDirty = true;
            return Value;
        }

        /// <summary>
        /// Subscribes to value changes
        /// </summary>
        /// <param name="callback">Callback to invoke when value changes</param>
        /// <param name="fireOnSubscribe">If true, the callback is invoked immediately with the current value upon subscription.</param>
        /// <returns>IDisposable to unsubscribe</returns>
        public IDisposable Subscribe(Action<T> callback, bool fireOnSubscribe = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            lock (_lock)
            {
                _subscribers.Add(callback);
            }

            if (fireOnSubscribe)
            {
                // Invoke the callback immediately with the current value
                callback(Value);
            }
            return new Subscription(this, callback);
        }

        /// <summary>
        /// Unsubscribes from value changes
        /// </summary>
        /// <param name="callback">Callback to remove</param>
        private void Unsubscribe(Action<T> callback)
        {
            lock (_lock)
            {
                _subscribers.Remove(callback);
            }
        }

        public IDisposable Subscribe(Action<object> callback, bool fireOnSubscribe = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            lock (_lock)
            {
                _objectSubscribers.Add(callback);
            }

            if (fireOnSubscribe)
            {
                // Invoke the callback immediately with the current value
                callback(Value);
            }

            return new ObjectSubscription(this, callback);
        }


        private void Unsubscribe(Action<object> callback)
        {
            lock (_lock)
            {
                _objectSubscribers.Remove(callback);
            }
        }

        public object GetValue() => Value;

        public void SetValue(object value)
        {
            throw new InvalidOperationException("Cannot set value of a computed property");
        }

        public void SetValue(object value, bool forceNotify = false)
        {
            throw new InvalidOperationException("Cannot set value of a computed property");
        }

        /// <summary>
        /// Disposes the computed property and clears all subscriptions
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _subscribers.Clear();
                _objectSubscribers.Clear();
            }
        }

        /// <summary>
        /// Recomputes the value
        /// </summary>
        private void RecomputeValue()
        {
            var newValue = _computation();
            if (!EqualityComparer<T>.Default.Equals(_cachedValue, newValue))
            {
                _cachedValue = newValue;
                NotifyValueChanged(newValue);
            }
            _isDirty = false;
        }

        /// <summary>
        /// Notifies subscribers of a value change
        /// </summary>
        /// <param name="newValue"></param>
        private void NotifyValueChanged(T newValue)
        {
            List<Action<T>> genericSubscribersCopy;
            List<Action<object>> objectSubscribersCopy;
            lock (_lock)
            {
                genericSubscribersCopy = new List<Action<T>>(_subscribers);
                objectSubscribersCopy = new List<Action<object>>(_objectSubscribers);
            }

            // Notification logic can be simplified as it's not expected to run off the main thread,
            // but we add the check for robustness.
            if (Flux.Manager != null && !FluxThreadManager.IsMainThread())
            {
                Flux.Threading.ExecuteOnMainThread(() =>
                {
                    foreach (var subscriber in genericSubscribersCopy) subscriber.Invoke(newValue);
                    foreach (var subscriber in objectSubscribersCopy) subscriber.Invoke(newValue);
                });
            }
            else
            {
                foreach (var subscriber in genericSubscribersCopy) subscriber.Invoke(newValue);
                foreach (var subscriber in objectSubscribersCopy) subscriber.Invoke(newValue);
            }
        }

        /// <summary>
        /// Implicit conversion from ComputedProperty to its value
        /// </summary>
        public static implicit operator T(ComputedProperty<T> property)
        {
            return property.Value;
        }

        private sealed class Subscription : IDisposable
        {
            private ComputedProperty<T> _parent;
            private Action<T> _action;
            public Subscription(ComputedProperty<T> parent, Action<T> action) { _parent = parent; _action = action; }
            public void Dispose() { _parent?.Unsubscribe(_action); _parent = null; _action = null; }
        }

        private sealed class ObjectSubscription : IDisposable
        {
            private ComputedProperty<T> _parent;
            private Action<object> _action;
            public ObjectSubscription(ComputedProperty<T> parent, Action<object> action) { _parent = parent; _action = action; }
            public void Dispose() { _parent?.Unsubscribe(_action); _parent = null; _action = null; }
        }
    }
}
