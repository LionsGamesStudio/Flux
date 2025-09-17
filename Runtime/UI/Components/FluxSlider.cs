using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Attributes;

namespace FluxFramework.UI
{
    /// <summary>
    /// A generic, reusable UI component that provides one-way or two-way binding for a Unity Slider.
    /// The property key and binding mode are configured directly in the inspector.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    [AddComponentMenu("Flux/UI/Flux Slider")]
    public class FluxSlider : FluxUIComponent
    {
        [Header("Component Reference")]
        [Tooltip("The Unity Slider component to control. If null, it will be found automatically.")]
        [SerializeField] private Slider sliderComponent;

        [Header("Binding Configuration")]
        [Tooltip("The Reactive Property Key to bind this slider's value to.")]
        [SerializeField] private string _propertyKey;

        [Tooltip("Defines the data flow direction. 'TwoWay' allows the slider to update the property.")]
        [SerializeField] private BindingMode _bindingMode = BindingMode.OneWay;

        // --- Private Binding Reference ---
        private SliderBinding _binding;

        /// <summary>
        /// Gets the reference to the Slider component.
        /// </summary>
        protected override void InitializeComponent()
        {
            if (sliderComponent == null)
            {
                sliderComponent = GetComponent<Slider>();
            }
        }

        /// <summary>
        /// Manually creates the binding for the slider based on the inspector configuration.
        /// </summary>
        protected override void RegisterCustomBindings()
        {
            if (string.IsNullOrEmpty(_propertyKey) || sliderComponent == null) return;

            // We create the specific SliderBinding instance.
            _binding = new SliderBinding(_propertyKey, sliderComponent);

            // Register it with the central system with default options.
            // A more advanced version could expose BindingOptions in the inspector.
            Flux.Manager.BindingSystem.Bind(_propertyKey, _binding, new BindingOptions { Mode = _bindingMode });

            // Track the binding for automatic cleanup.
            TrackBinding(_binding);
        }

        /// <summary>
        /// Applies the global theme to the different parts of the slider.
        /// </summary>
        public override void ApplyTheme()
        {
            base.ApplyTheme();

            var theme = UIThemeManager.CurrentTheme;
            if (theme == null || sliderComponent == null) return;

            // Apply theme colors to the slider's fill and handle images.
            if (sliderComponent.fillRect != null)
            {
                var fillImage = sliderComponent.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = theme.accentColor;
                }
            }

            if (sliderComponent.handleRect != null)
            {
                var handleImage = sliderComponent.handleRect.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = theme.primaryColor;
                }
            }
        }

        #region Public API

        public float GetCurrentValue() => sliderComponent?.value ?? 0f;
        public void SetValueWithoutNotify(float value) { sliderComponent?.SetValueWithoutNotify(value); }
        public void SetRange(float min, float max) { if (sliderComponent != null) { sliderComponent.minValue = min; sliderComponent.maxValue = max; } }
        public Vector2 GetRange() => sliderComponent != null ? new Vector2(sliderComponent.minValue, sliderComponent.maxValue) : Vector2.zero;

        #endregion
    }
}