using System;
using UnityEngine;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    public interface IFluxComponentRegistry
    {
        /// <summary>
        /// Event fired when a new component type is registered.
        /// </summary>
        event Action<Type, FluxComponentAttribute> OnComponentTypeRegistered;

        /// <summary>
        /// Event fired when a new component instance is registered.
        /// </summary>
        event Action<MonoBehaviour, FluxComponentAttribute> OnComponentInstanceRegistered;

        /// <summary>
        /// Initializes the component registry and scans for components in the current scene.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Registers a component instance with the registry.
        /// </summary>
        /// <param name="component"></param>
        void RegisterComponentInstance(MonoBehaviour component);

        /// <summary>
        /// Registers all components in the currently active scene.
        /// </summary>
        void RegisterAllComponentsInScene();

        /// <summary>
        /// Clears the instance cache.
        /// </summary>
        void ClearInstanceCache();

        /// <summary>
        /// Clears the component cache.
        /// </summary>
        void ClearCache();
    }
}