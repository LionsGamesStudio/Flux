using UnityEngine;
using FluxFramework.Attributes;
using System;
using System.Collections.Generic;

namespace FluxFramework.Core
{
    [FluxComponent(AutoRegister = true)]
    public abstract class FluxMonoBehaviour : MonoBehaviour, IFluxReactiveObject
    {
        [NonSerialized] private List<string> _registeredProperties = new List<string>();
        [NonSerialized] private bool _isFluxAwakeCalled = false;
        [NonSerialized] private bool _isFluxStartCalled = false;

        #region Sealed Unity Lifecycle

        protected void Awake() { /* The role is now managed by the registry */ }
        protected void Start() { /* The role is now managed by the registry */ }

        protected void OnDestroy()
        {
            if (_isFluxAwakeCalled)
            {
                OnFluxDestroy();
            }
            CleanupReactiveProperties();
        }

        #endregion

        #region Property Management

        public void InitializeReactiveProperties(IFluxManager manager)
        {
            if (_registeredProperties.Count > 0) return;
            var registeredKeys = manager.PropertyFactory.RegisterPropertiesFor(this);
            _registeredProperties = new List<string>(registeredKeys);
            OnFluxPropertiesInitialized();
        }

        private void CleanupReactiveProperties()
        {
            if (Flux.Manager == null || Flux.Manager.Properties == null || _registeredProperties.Count == 0) return;
            foreach (var propertyKey in _registeredProperties)
            {
                Flux.Manager.Properties.UnregisterProperty(propertyKey);
            }
            _registeredProperties.Clear();
        }

        #endregion

        #region Internal Initialization Flow (Called by the Registry)

        /// <summary>
        /// Trigger the OnFluxAwake lifecycle method. Called by the registry.
        /// </summary>
        public void TriggerFluxAwake()
        {
            if (_isFluxAwakeCalled) return;
            _isFluxAwakeCalled = true;
            OnFluxAwake();
        }

        /// <summary>
        /// Trigger the OnFluxStart lifecycle method. Called by the registry.
        /// </summary>
        public void TriggerFluxStart()
        {
            if (_isFluxStartCalled) return;
            if (!_isFluxAwakeCalled) TriggerFluxAwake(); 
            _isFluxStartCalled = true;
            OnFluxStart();
        }

        #endregion

        #region Protected Virtual Lifecycle

        protected virtual void OnFluxPropertiesInitialized() { }
        protected virtual void OnFluxAwake() { }
        protected virtual void OnFluxStart() { }
        protected virtual void OnFluxDestroy() { }

        #endregion
    }
}