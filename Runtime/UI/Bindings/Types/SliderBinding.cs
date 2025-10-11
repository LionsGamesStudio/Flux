using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Binding
{
    [BindingFor(typeof(Slider))]
    public class SliderBinding : UIBinding<float>
    {
        private readonly Slider _slider;
        private IReactiveProperty<float> _property; // This will be set by the system
        private bool _isInitialized = false; // Flag for the initial setup

        public SliderBinding(string propertyKey, Slider slider)
            : base(propertyKey, slider)
        {
            _slider = slider;
        }

        /// <summary>
        /// This method is called by the ReactiveBindingSystem once it has the definitive
        /// ReactiveProperty instance. This is where we activate the binding logic.
        /// </summary>
        public override void Activate(ReactiveProperty<float> property)
        {
            if (property == null) return;
            _property = property;

            // Activate TwoWay binding if required by the options
            if (Options.Mode == Attributes.BindingMode.TwoWay || Options.Mode == Attributes.BindingMode.OneWayToSource)
            {
                _slider?.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        /// <summary>
        /// Updates the UI from the property's value.
        /// On the very first update, it also configures the slider's maxValue.
        /// </summary>
        public override void UpdateUI(float value)
        {
            if (_slider == null) return;

            // --- Set Max Value on Init ---
            if (!_isInitialized)
            {
                // If the initial value is greater than the current max, assume it's the new max.
                if (value > _slider.maxValue)
                {
                    _slider.maxValue = value;
                }
                _isInitialized = true;
            }

            // Always update the current value without triggering onValueChanged listeners.
            _slider.SetValueWithoutNotify(value);
        }

        public override float GetUIValue() => _slider?.value ?? 0f;

        public override void Dispose()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            base.Dispose();
        }

        private void OnSliderValueChanged(float uiValue)
        {
            if (_property != null && IsActive)
            {
                // The converter logic is handled by the Transform extension,
                // so we can assume _property is the correct final type.
                _property.Value = uiValue;
            }
        }
    }
}