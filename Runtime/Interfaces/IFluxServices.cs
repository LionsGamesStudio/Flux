using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    // Defines the contract for the central property management service.
    /// </summary>
    public interface IFluxPropertyManager
    {
        /// <summary>
        /// Event triggered when a new property is registered.
        /// </summary>
        event Action<string, IReactiveProperty> OnPropertyRegistered;

        /// <summary>
        /// Registers a new property with the specified key. If a property with the same key already exists, it will be overwritten.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="property"></param>
        /// <param name="isPersistent"></param>
        void RegisterProperty(string key, IReactiveProperty property, bool isPersistent);

        /// <summary>
        /// Gets a registered property by key. Returns null if the property does not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IReactiveProperty GetProperty(string key);

        /// <summary>
        /// Gets a registered property by key. Throws an exception if the property does not exist or if the type does not match.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        ReactiveProperty<T> GetProperty<T>(string key);

        /// <summary>
        /// Gets an existing property by key or creates a new one with the specified default value if it doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        ReactiveProperty<T> GetOrCreateProperty<T>(string key, T defaultValue = default);

        /// <summary>
        /// Checks if a property with the specified key is registered.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool HasProperty(string key);

        /// <summary>
        /// Unregisters a property by key. Returns true if the property was successfully unregistered, false if the property did not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool UnregisterProperty(string key);

        /// <summary>
        /// Gets the unique key associated with the given property instance.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        string GetKey(IReactiveProperty property);

        /// <summary>
        /// Retrieves all registered property keys.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetAllPropertyKeys();

        /// <summary>
        /// Clears all non-persistent properties from the manager.
        /// </summary>
        void ClearNonPersistentProperties();

        /// <summary>
        /// Subscribes to a property by key. If the property does not exist at the time of subscription,
        /// the subscription will be deferred until the property is registered. The provided callback will be invoked
        /// with the property instance once it becomes available.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="onSubscribe"></param>
        /// <returns></returns>
        IDisposable SubscribeDeferred(string key, Action<IReactiveProperty> onSubscribe);

        /// <summary>
        /// Gets the total number of registered properties.
        /// </summary>
        int PropertyCount { get; }
    }

    /// <summary>
    /// Defines the contract for the core framework manager.
    /// This exposes the public services that other parts of the application can interact with.
    /// </summary>
    public interface IFluxManager
    {
        /// <summary>
        /// Indicates whether the framework has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Provides access to the central property management service.
        /// </summary>
        IFluxPropertyManager Properties { get; }

        /// <summary>
        /// Provides access to the thread management service.
        /// </summary>
        IFluxThreadManager Threading { get; }

        /// <summary>
        /// Starts a coroutine using the framework's internal MonoBehaviour.
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        Coroutine StartCoroutine(IEnumerator routine);

        /// <summary>
        /// Stops a running coroutine.
        /// </summary>
        /// <param name="routine"></param>
        void StopCoroutine(Coroutine routine);
    }

    /// <summary>
    /// Defines the contract for the thread management service.
    /// </summary>
    public interface IFluxThreadManager
    {
        /// <summary>
        /// Executes the provided action on the main Unity thread.
        /// </summary>
        /// <param name="action"></param>
        void ExecuteOnMainThread(Action action);
        
        /// <summary>
        /// Checks if the current thread is the main Unity thread.
        /// </summary>
        /// <returns>True if called from the main thread, otherwise false.</returns>
        bool IsMainThread();
    }
}