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

            Bind(); // Call the public Bind method
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
                    MethodInfo bindMethod = typeof(ReactiveBindingSystem).GetMethod("Bind").MakeGenericMethod(bindingValueType);
                    bindMethod.Invoke(null, new object[] { bindingAttr.PropertyKey, newBinding, options });
                    
                    TrackBinding(newBinding);
                }
            }
        }
        
        private IUIBinding CreateBindingForComponent(Component uiComponent, FluxBindingAttribute bindingAttr)
        {
            var options = bindingAttr.CreateOptions();
            bool isTwoWay = options.Mode == BindingMode.TwoWay || options.Mode == BindingMode.OneWayToSource;

            if (uiComponent is TextMeshProUGUI tmpText) return new TextBinding(bindingAttr.PropertyKey, tmpText);
            if (uiComponent is Text legacyText) return new LegacyTextBinding(bindingAttr.PropertyKey, legacyText);
            if (uiComponent is Slider slider) return new SliderBinding(bindingAttr.PropertyKey, slider, isTwoWay);
            if (uiComponent is Toggle toggle) return new ToggleBinding(bindingAttr.PropertyKey, toggle, isTwoWay);
            if (uiComponent is Image image)
            {
                return bindingAttr.PropertyKey.ToLower().Contains("sprite") 
                    ? new SpriteBinding(bindingAttr.PropertyKey, image) 
                    : new ColorBinding(bindingAttr.PropertyKey, image);
            }
            
            Debug.LogWarning($"[FluxFramework] No automatic binding found for component type '{uiComponent.GetType().Name}'. Register it manually in RegisterCustomBindings().", this);
            return null;
        }

        private void UnregisterAllBindings()
        {
            foreach (var binding in _activeBindings)
            {
                ReactiveBindingSystem.Unbind(binding.PropertyKey, binding);
            }
            _activeBindings.Clear();
        }
    }
}