using UnityEngine;
using FluxFramework.Attributes;
using System;

namespace FluxFramework.Core
{
    /// <summary>
    /// The base class for all Flux-aware MonoBehaviours. It provides a safe, framework-aware
    /// lifecycle. 
    /// It ensures that all Flux components are initialized only after the Flux Framework is fully set up.
    /// It also provides helper methods for interacting with reactive properties and the event bus.
    /// </summary>
    [FluxComponent(AutoRegister = true)]
    public abstract class FluxMonoBehaviour : MonoBehaviour
    {
        private bool _isFrameworkInitialized = false;

        #region Sealed Unity Lifecycle (DO NOT OVERRIDE)

        /// <summary>
        /// This Awake method is sealed by being non-virtual. It handles the core initialization logic.
        /// DO NOT declare a new Awake() method in child classes. Use OnFluxAwake() instead.
        /// </summary>
        protected void Awake()
        {
            if (FluxManager.Instance != null && FluxManager.Instance.IsInitialized)
            {
                FrameworkInitialize();
            }
            else
            {
                FluxManager.OnFrameworkInitialized += FrameworkInitializeOnce;
            }
        }

        /// <summary>
        /// This Start method is sealed by being non-virtual.
        /// DO NOT declare a new Start() method in child classes. Use OnFluxStart() instead.
        /// </summary>
        protected void Start()
        {
            // This method is intentionally left empty.
            // Its purpose is to exist so that Unity calls it, but the user-facing logic is in OnFluxStart().
        }

        /// <summary>
        /// This OnDestroy method is sealed by being non-virtual. It handles core cleanup.
        /// DO NOT declare a new OnDestroy() method in child classes. Use OnFluxDestroy() instead.
        /// </summary>
        protected void OnDestroy()
        {
            FluxManager.OnFrameworkInitialized -= FrameworkInitializeOnce;
            
            if (_isFrameworkInitialized)
            {
                OnFluxDestroy();
            }
        }

        #endregion

        #region Private Initialization Flow

        private void FrameworkInitializeOnce()
        {
            FluxManager.OnFrameworkInitialized -= FrameworkInitializeOnce;
            FrameworkInitialize();
        }



        private void FrameworkInitialize()
        {
            if (_isFrameworkInitialized) return;
            _isFrameworkInitialized = true;
            
            // Call the safe, overridable lifecycle methods for child classes.
            OnFluxAwake();
            OnFluxStart();
        }
        
        #endregion

        #region Protected Virtual (Overridable Lifecycle Methods for Child Classes)

        /// <summary>
        /// This is the framework-safe equivalent of Awake().
        /// It is guaranteed to be called only once and after the Flux Framework is fully initialized.
        /// Override this for component setup and property initialization/subscription.
        /// </summary>
        protected virtual void OnFluxAwake() { }

        /// <summary>
        /// This is the framework-safe equivalent of Start().
        /// It is guaranteed to be called after OnFluxAwake().
        /// Override this for logic that may depend on other components being initialized.
        /// </summary>
        protected virtual void OnFluxStart() { }
        
        /// <summary>
        /// This is the framework-safe equivalent of OnDestroy().
        /// Override this for all your cleanup logic, such as disposing of subscriptions.
        /// </summary>
        protected virtual void OnFluxDestroy() { }
        
        #endregion

        #region Helper Methods

        protected void UpdateReactiveProperty<T>(string propertyKey, T newValue)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = newValue;
            }
            else
            {
                Debug.LogWarning($"[FluxFramework] Attempted to update property '{propertyKey}', but it does not exist in the FluxManager.", this);
            }
        }

        protected T GetReactivePropertyValue<T>(string propertyKey)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }
        
        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T> onChanged, bool fireOnSubscribe = false)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }
        
        protected IDisposable SubscribeToProperty<T>(string propertyKey, Action<T, T> onChanged, bool fireOnSubscribe = false)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property?.Subscribe(onChanged, fireOnSubscribe);
        }

        protected void PublishEvent<T>(T eventData) where T : IFluxEvent
        {
            EventBus.Publish(eventData);
        }

        #endregion
    }
}