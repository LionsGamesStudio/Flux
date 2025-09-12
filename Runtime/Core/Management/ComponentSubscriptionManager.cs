using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// A helper component that is automatically added to GameObjects to manage the lifecycle
    /// of IDisposable subscriptions created by framework attributes ([FluxPropertyChangeHandler]).
    /// It ensures that all subscriptions are disposed when the GameObject is destroyed.
    /// </summary>
    [DisallowMultipleComponent]
    internal class ComponentSubscriptionManager : MonoBehaviour
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public void Add(IDisposable subscription)
        {
            if (subscription != null)
            {
                _subscriptions.Add(subscription);
            }
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}