using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Attributes;

namespace FluxFramework.UI
{
    /// <summary>
    /// A generic, reusable UI component that provides one-way or two-way binding for a Unity Toggle.
    /// The property key and binding mode are configured directly in the inspector.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class FluxToggle : FluxUIComponent
    {
        [Header("Component Reference")]
        [Tooltip("The Unity Toggle component to control. If null, it will be found automatically.")]
        [SerializeField] private Toggle toggleComponent;

        [Header("Binding Configuration")]
        [Tooltip("The Reactive Property Key to bind this toggle's 'isOn' state to.")]
        [SerializeField] private string _propertyKey;
        
        [Tooltip("Defines the data flow direction. 'TwoWay' allows the toggle to update the property.")]
        [SerializeField] private BindingMode _bindingMode = BindingMode.OneWay;

        // --- Private Binding Reference ---
        private ToggleBinding _binding;

        /// <summary>
        /// Gets the reference to the Toggle component.
        /// </summary>
        protected override void InitializeComponent()
        {
            if (toggleComponent == null)
            {
                toggleComponent = GetComponent<Toggle>();
            }
        }
        
        /// <summary>
        /// Manually creates the binding for the toggle based on the inspector configuration.
        /// </summary>
        protected override void RegisterCustomBindings()
        {
            if (string.IsNullOrEmpty(_propertyKey) || toggleComponent == null) return;
            
            _binding = new ToggleBinding(_propertyKey, toggleComponent);
            
            // The binding options are passed to the system, which will then configure the binding.
            ReactiveBindingSystem.Bind(_propertyKey, _binding, new BindingOptions { Mode = _bindingMode });
            
            TrackBinding(_binding);
        }
        
        /// <summary>
        /// Applies the global theme to the different parts of the toggle.
        /// </summary>
        public override void ApplyTheme()
        {
            base.ApplyTheme();
            
            var theme = UIThemeManager.CurrentTheme;
            if (theme == null || toggleComponent == null) return;
            
            // Apply theme colors to the toggle's background and checkmark images.
            if (toggleComponent.targetGraphic != null)
            {
                toggleComponent.targetGraphic.color = theme.secondaryColor; // Example: use secondary for background
            }
            
            if (toggleComponent.graphic != null) // This is usually the checkmark
            {
                toggleComponent.graphic.color = theme.accentColor;
            }
        }

        #region Public API
        
        public bool GetCurrentValue() => toggleComponent?.isOn ?? false;
        public void SetValueWithoutNotify(bool value) { toggleComponent?.SetIsOnWithoutNotify(value); }
        
        #endregion
    }
}