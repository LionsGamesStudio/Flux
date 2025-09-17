using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Events;

namespace FluxFramework.Core
{
    /// <summary>
    /// Thread-safe reactive property that notifies subscribers when its value changes.
    /// Subscriptions are managed via the IDisposable pattern.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    public class ReactiveProperty<T> : IReactiveProperty<T>
    {
        private T _value;
        private readonly object _lock = new object();
        private readonly List<Action<T>> _subscribers = new List<Action<T>>();
        private readonly List<Action<object>> _objectSubscribers = new List<Action<object>>();
        private readonly List<Action<T, T>> _subscribersWithOldValue = new List<Action<T, T>>();
        private List<IDisposable> _dependentSubscriptions;

        public Type ValueType => typeof(T);

        /// <summary>
        /// Gets whether this property has any subscribers.
        /// </summary>
        public bool HasSubscribers
        {
            get
            {
                lock (_lock)
                {
                    return _subscribers.Count > 0 || _objectSubscribers.Count > 0;
                }
            }
        }

        /// <summary>
        /// Gets the number of subscribers.
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
        /// The current value of the property.
        /// Setting this value will notify all subscribers if the new value is different.
        /// </summary>
        public virtual T Value { get => GetValueInternal(); set => SetValueInternal(value, true); }

        /// <summary>
        /// Gets the current value in a thread-safe manner.
        /// </summary>
        /// <returns></returns>
        private T GetValueInternal() { lock (_lock) { return _value; } }

        /// <summary>
        /// Creates a new reactive property with an initial value.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        public ReactiveProperty(T initialValue = default)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Subscribes to value changes.
        /// </summary>
        /// <param name="callback">The callback to invoke when the value changes.</param>
        /// <param name="fireOnSubscribe">If true, the callback will be immediately invoked with the current value.</param>
        /// <returns>An IDisposable object that can be used to unsubscribe.</returns>
        public IDisposable Subscribe(Action<T> callback, bool fireOnSubscribe = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                _subscribers.Add(callback);
            }

            if (fireOnSubscribe)
            {
                callback.Invoke(GetValueInternal());
            }

            return new Subscription(this, callback);
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
                callback.Invoke(GetValueInternal());
            }
            return new ObjectSubscription(this, callback);
        }

        public IDisposable Subscribe(Action<T, T> callback, bool fireOnSubscribe = false)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            lock (_lock)
            {
                _subscribersWithOldValue.Add(callback);
            }
            if (fireOnSubscribe)
            {
                var currentValue = GetValueInternal();
                callback.Invoke(currentValue, currentValue); // Pass current value as old and new for the initial fire.
            }
            return new SubscriptionWithOldValue(this, callback);
        }

        // This method is now private as its only purpose is to be called by the Subscription's Dispose method.
        private void Unsubscribe(Action<T> callback)
        {
            if (callback == null) return;

            lock (_lock)
            {
                _subscribers.Remove(callback);
            }
        }

        // This method is now private as its only purpose is to be called by the ObjectSubscription's Dispose method.
        private void Unsubscribe(Action<object> callback)
        {
            if (callback == null) return;

            lock (_lock)
            {
                _objectSubscribers.Remove(callback);
            }
        }

        private void Unsubscribe(Action<T, T> callback)
        {
            if (callback == null) return;
            lock (_lock)
            {
                _subscribersWithOldValue.Remove(callback);
            }
        }

        /// <summary>
        /// Gets the current value as an object.
        /// </summary>
        /// <returns>The current value.</returns>
        public object GetValue() => Value;

        /// <summary>
        /// Sets the value from an object.
        /// </summary>
        /// <param name="value">The new value.</param>
        public void SetValue(object value)
        {
            SetValue(value, false);
        }

        /// <summary>
        /// Sets the value from an object with notification control.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="forceNotify">Whether to force notification even if the value hasn't changed.</param>
        public void SetValue(object value, bool forceNotify = false)
        {
            if (value is T typedValue)
            {
                SetValueInternal(typedValue, forceNotify);
            }
            else if (value == null && !typeof(T).IsValueType)
            {
                SetValueInternal(default, forceNotify);
            }
            else
            {
                UnityEngine.Debug.LogError($"[FluxFramework] Value '{value}' of type '{value?.GetType()}' cannot be assigned to ReactiveProperty of type '{typeof(T)}'.");
            }
        }
        
        /// <summary>
        /// Registers a subscription that this property depends on. When this property
        /// is disposed, it will also dispose of all its dependent subscriptions.
        /// This is used by extension methods like Transform() and CombineWith().
        /// </summary>
        public void AddDependentSubscription(IDisposable subscription)
        {
            lock (_lock)
            {
                if (_dependentSubscriptions == null)
                {
                    _dependentSubscriptions = new List<IDisposable>();
                }
                _dependentSubscriptions.Add(subscription);
            }
        }

        /// <summary>
        /// Disposes the reactive property and clears all direct and dependent subscriptions.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                _subscribers.Clear();
                _objectSubscribers.Clear();
                _subscribersWithOldValue.Clear();

                if (_dependentSubscriptions != null)
                {
                    foreach (var sub in _dependentSubscriptions)
                    {
                        sub.Dispose();
                    }
                    _dependentSubscriptions.Clear();
                }
            }
        }

        private void SetValueInternal(T newValue, bool forceNotify)
        {
            T oldValue;
            bool valueChanged;

            lock (_lock)
            {
                oldValue = _value;
                valueChanged = !EqualityComparer<T>.Default.Equals(oldValue, newValue);
                if (valueChanged)
                {
                    _value = newValue;
                }
            }

            if (forceNotify || valueChanged)
            {
                // Pass both old and new value to the notification method.
                NotifyValueChanged(oldValue, newValue);
            }
        }

        private void NotifyValueChanged(T oldValue, T newValue)
        {
            List<Action<T>> genericSubscribersCopy;
            List<Action<object>> objectSubscribersCopy;
            List<Action<T, T>> subscribersWithOldValueCopy;

            lock (_lock)
            {
                genericSubscribersCopy = new List<Action<T>>(_subscribers);
                objectSubscribersCopy = new List<Action<object>>(_objectSubscribers);
                subscribersWithOldValueCopy = new List<Action<T, T>>(_subscribersWithOldValue);
            }

            Action notificationAction = () =>
            {
                foreach (var subscriber in genericSubscribersCopy) subscriber.Invoke(newValue);
                foreach (var subscriber in objectSubscribersCopy) subscriber.Invoke(newValue);
                foreach (var subscriber in subscribersWithOldValueCopy) subscriber.Invoke(oldValue, newValue);
            };

            if (Flux.Manager != null && !Flux.Manager.Threading.IsMainThread())
            {
                Flux.Manager.Threading.ExecuteOnMainThread(notificationAction);
            }
            else
            {
                notificationAction();
            }

            // Optionally, raise a PropertyChangedEvent here if needed.
            var key = ((FluxManager)Flux.Manager).Properties.GetKey(this);
            EventBus.Publish(new PropertyChangedEvent(key, oldValue, newValue, typeof(T)));
        }

        public static implicit operator T(ReactiveProperty<T> property)
        {
            return property.Value;
        }

        // Nested private class to manage the lifecycle of a generic subscription.
        private sealed class Subscription : IDisposable
        {
            private ReactiveProperty<T> _parent;
            private Action<T> _action;

            public Subscription(ReactiveProperty<T> parent, Action<T> action)
            {
                _parent = parent;
                _action = action;
            }

            public void Dispose()
            {
                if (_parent != null)
                {
                    _parent.Unsubscribe(_action);
                    _parent = null;
                    _action = null;
                }
            }
        }

        // Nested private class to manage the lifecycle of a non-generic subscription.
        private sealed class ObjectSubscription : IDisposable
        {
            private ReactiveProperty<T> _parent;
            private Action<object> _action;

            public ObjectSubscription(ReactiveProperty<T> parent, Action<object> action)
            {
                _parent = parent;
                _action = action;
            }

            public void Dispose()
            {
                if (_parent != null)
                {
                    _parent.Unsubscribe(_action);
                    _parent = null;
                    _action = null;
                }
            }
        }
        
        private sealed class SubscriptionWithOldValue : IDisposable
        {
            private ReactiveProperty<T> _parent;
            private Action<T, T> _action;

            public SubscriptionWithOldValue(ReactiveProperty<T> parent, Action<T, T> action)
            {
                _parent = parent;
                _action = action;
            }

            public void Dispose()
            {
                _parent?.Unsubscribe(_action);
                _parent = null;
                _action = null;
            }
        }
    }
}
