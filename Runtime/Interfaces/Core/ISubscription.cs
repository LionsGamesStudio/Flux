using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for disposable subscriptions
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Whether this subscription is still active
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// The subscriber object
        /// </summary>
        object Subscriber { get; }
    }
}
