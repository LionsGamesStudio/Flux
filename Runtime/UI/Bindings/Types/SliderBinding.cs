using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    public class SliderBinding : UIBinding<float>
    {
        private readonly Slider _slider;
        private readonly bool _isTwoWay;
        private IReactiveProperty<float> _property;

        public SliderBinding(string propertyKey, Slider slider, bool isTwoWay) 
            : base(propertyKey, slider)
        {
            _slider = slider;
            _isTwoWay = isTwoWay;

            if (_isTwoWay && _slider != null)
            {
                _property = FluxManager.Instance.GetOrCreateProperty<float>(propertyKey);
                _slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        public override void UpdateUI(float value)
        {
            if (_slider != null) _slider.SetValueWithoutNotify(value);
        }

        public override float GetUIValue() => _slider?.value ?? 0f;

        public override void Dispose()
        {
            if (_isTwoWay && _slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            base.Dispose();
        }

        private void OnSliderValueChanged(float uiValue)
        {
            if (_property != null)
            {
                object finalValue = Options.Converter != null ? Options.Converter.ConvertBack(uiValue) : uiValue;
                _property.Value = (float)finalValue;
            }
        }
        
        public override void UpdateToProperty()
        {
            if(_slider != null) OnSliderValueChanged(_slider.value);
        }
    }
}