using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// Base class for MonoBehaviours that integrate with the Flux Framework.
    /// </summary>
    [FluxComponent(AutoRegister = true)] // Add the attribute here!
    public abstract class FluxMonoBehaviour : MonoBehaviour
    {
        private bool _isInitialized = false;

        /// <summary>
        /// Unity's Awake method. Child classes should override this and call base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            // The core initialization logic for all Flux MonoBehaviours.
            // This ensures that framework-dependent logic runs at the correct time.
            if (FluxManager.Instance != null && FluxManager.Instance.IsInitialized)
            {
                Initialize();
            }
            else
            {
                FluxManager.OnFrameworkInitialized += InitializeOnce;
            }
        }

        protected virtual void Start()
        {
            // Child classes can override this method for start logic.
        }

        /// <summary>
        /// Unity's OnDestroy method. Child classes should override this and call base.OnDestroy().
        /// </summary>
        protected virtual void OnDestroy()
        {
            // The core cleanup logic for all Flux MonoBehaviours.
            FluxManager.OnFrameworkInitialized -= InitializeOnce;
            if (_isInitialized)
            {
                Cleanup();
            }
        }

        private void InitializeOnce()
        {
            FluxManager.OnFrameworkInitialized -= InitializeOnce;
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            
            // This is where any base initialization logic would go.
            
            _isInitialized = true;
        }

        private void Cleanup()
        {
            // This is where any base cleanup logic would go.
        }
        
        /// <summary>
        /// Update the value of a reactive property in the FluxManager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyKey"></param>
        /// <param name="newValue"></param>
        protected void UpdateReactiveProperty<T>(string propertyKey, T newValue)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            if (property != null)
            {
                property.Value = newValue;
            }
        }

        /// <summary>
        /// Get the current value of a reactive property from the FluxManager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyKey"></param>
        /// <returns></returns>
        protected T GetReactivePropertyValue<T>(string propertyKey)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            return property != null ? property.Value : default(T);
        }

        /// <summary>
        /// Subscribe to changes of a reactive property in the FluxManager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyKey"></param>
        /// <param name="onChanged"></param>
        protected void SubscribeToProperty<T>(string propertyKey, System.Action<T> onChanged)
        {
            var property = FluxManager.Instance.GetProperty<T>(propertyKey);
            property?.Subscribe(onChanged);
        }

        /// <summary>
        /// Unsubscribe from changes of a reactive property in the FluxManager.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventData"></param>
        protected void PublishEvent<T>(T eventData) where T : IFluxEvent
        {
            EventBus.Publish(eventData);
        }
    }
}