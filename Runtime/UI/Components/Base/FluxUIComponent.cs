using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Attributes;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;

namespace FluxFramework.UI
{
    /// <summary>
    /// The base class for all UI components. It provides a guaranteed automatic binding system
    /// and a dedicated virtual method for derived classes to add their own custom binding logic.
    /// </summary>
    public abstract class FluxUIComponent : FluxMonoBehaviour
    {
        [Header("Theming")]
        [Tooltip("If true, this component will attempt to apply the global UI theme on start.")]
        [SerializeField] private bool _applyThemeOnStart = true;

        private readonly List<IUIBinding> _activeBindings = new List<IUIBinding>();
        private bool _isBound = false;

        // --- PUBLIC, NON-VIRTUAL (SEALED) API ---

        /// <summary>
        /// Initializes and registers all bindings for this component.
        /// This is the public, non-overridable entry point. It can be safely called multiple times.
        /// </summary>
        public void Bind()
        {
            if (_isBound) return;

            RegisterAllBindingsByAttribute();
            RegisterCustomBindings();
            _isBound = true;
        }

        /// <summary>
        /// Unregisters and cleans up all active bindings for this component.
        /// This is the public, non-overridable entry point.
        /// </summary>
        public void Unbind()
        {
            if (!_isBound) return;
            UnregisterAllBindings();
            _isBound = false;
        }


        // --- PROTECTED, VIRTUAL EXTENSION POINTS ---

        /// <summary>
        /// This virtual method is the designated entry point for derived classes to perform their
        /// component-specific initialization, like getting references with GetComponent.
        /// It is called automatically within Awake.
        /// </summary>
        protected virtual void InitializeComponent() { }

        /// <summary>
        /// This virtual method is the designated entry point for derived classes to add
        /// custom binding logic that cannot be handled by attributes alone.
        /// </summary>
        protected virtual void RegisterCustomBindings() { }

        /// <summary>
        /// This virtual method is the designated entry point for derived classes to perform
        /// any specific cleanup needed before destruction.
        /// It is called automatically within OnDestroy, before bindings are unregistered.
        /// </summary>
        protected virtual void CleanupComponent() { }

        /// <summary>
        /// Applies the currently active UITheme to this component.
        /// Derived classes should override this to style their specific elements
        /// (e.g., a FluxButton would style its background image and text).
        /// </summary>
        public virtual void ApplyTheme() {}

        /// <summary>
        /// Helper method for derived classes to add their manually created bindings
        /// to the central tracking list for automatic cleanup.
        /// </summary>
        protected void TrackBinding(IUIBinding binding)
        {
            if (binding != null && !_activeBindings.Contains(binding))
            {
                _activeBindings.Add(binding);
            }
        }


        // --- INTERNAL & LIFECYCLE MANAGEMENT ---

        /// <summary>
        /// The Awake method is sealed and controls the lifecycle.
        /// </summary>
        protected sealed override void OnFluxAwake()
        {
            base.OnFluxAwake();
            InitializeComponent();

            // Apply the theme after initialization but before binding.
            if (_applyThemeOnStart)
            {
                ApplyTheme();
            }
        }

        /// <summary>
        /// The OnDestroy method is sealed and guarantees cleanup.
        /// </summary>
        protected sealed override void OnFluxDestroy()
        {
            Unbind(); // Call the public Unbind method
            CleanupComponent();
            base.OnFluxDestroy();
        }

        private void RegisterAllBindingsByAttribute()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var bindingAttr = field.GetCustomAttribute<FluxBindingAttribute>();
                if (bindingAttr == null) continue;

                var uiComponent = field.GetValue(this) as Component;
                if (uiComponent == null) continue;

                IUIBinding newBinding = CreateBindingForComponent(uiComponent, bindingAttr);
                
                if (newBinding != null)
                {
                    var options = bindingAttr.CreateOptions();
                    
                    // Use reflection to call the generic Bind<T> method
                    Type bindingValueType = newBinding.ValueType;
                    MethodInfo bindMethod = typeof(IReactiveBindingSystem).GetMethod("Bind").MakeGenericMethod(bindingValueType);
                    bindMethod.Invoke(Flux.Manager.BindingSystem, new object[] { bindingAttr.PropertyKey, newBinding, options });
                    
                    TrackBinding(newBinding);
                }
            }
        }
        
        private IUIBinding CreateBindingForComponent(Component uiComponent, FluxBindingAttribute bindingAttr)
        {
            var newBinding = Flux.Manager.BindingFactory.Create(bindingAttr.PropertyKey, uiComponent);

            if (newBinding == null)
            {
                Flux.Manager.Logger.Warning($"No binding creator found for component type '{uiComponent.GetType().Name}'. Did you forget to add the [BindingFor(typeof(...))] attribute to your binding class?", this);
            }

            return newBinding;
        }

        private void UnregisterAllBindings()
        {
            foreach (var binding in _activeBindings)
            {
                Flux.Manager.BindingSystem.Unbind(binding.PropertyKey, binding);
            }
            _activeBindings.Clear();
        }
    }
}