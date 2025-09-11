using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

namespace FluxFramework.UI
{
    /// <summary>
    /// A UI component that provides reactive binding for a Unity Slider.
    /// Binding is handled automatically by the base class.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class FluxSlider : FluxUIComponent
    {
        [Header("Component References")]
        [Tooltip("The Unity Slider component. If null, it will be found automatically.")]
        [SerializeField] private Slider sliderComponent;

        // --- Declarative Binding ---
        [Header("Binding Configuration")]
        [Tooltip("Assign the Slider component here to configure its data binding.")]
        [FluxBinding("ui.slider.value", Mode = BindingMode.TwoWay)]
        [SerializeField] private Slider _valueBindingTarget;

        protected override void InitializeComponent()
        {
            if (sliderComponent == null)
            {
                sliderComponent = GetComponent<Slider>();
            }
            if (_valueBindingTarget == null)
            {
                _valueBindingTarget = sliderComponent;
            }
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();
            
            var theme = UIThemeManager.CurrentTheme;
            if (theme == null) return;

            // Apply colors from theme
            if (sliderComponent != null)
            {
                // Background
                if (sliderComponent.fillRect != null && theme.accentColor != null)
                {
                    var fillImage = sliderComponent.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.color = theme.accentColor;
                    }
                }

                // Handle
                if (sliderComponent.handleRect != null && theme.primaryColor != null)
                {
                    var handleImage = sliderComponent.handleRect.GetComponent<Image>();
                    if (handleImage != null)
                    {
                        handleImage.color = theme.primaryColor;
                    }
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