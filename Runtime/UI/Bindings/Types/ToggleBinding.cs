using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    public class ToggleBinding : UIBinding<bool>
    {
        private readonly Toggle _toggle;
        private readonly bool _isTwoWay;
        private IReactiveProperty<bool> _property;

        public ToggleBinding(string propertyKey, Toggle toggle, bool isTwoWay) 
            : base(propertyKey, toggle)
        {
            _toggle = toggle;
            _isTwoWay = isTwoWay;

            if (_isTwoWay && _toggle != null)
            {
                _property = FluxManager.Instance.GetOrCreateProperty<bool>(propertyKey);
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        public override void UpdateUI(bool value)
        {
            if (_toggle != null) _toggle.SetIsOnWithoutNotify(value);
        }

        public override bool GetUIValue() => _toggle?.isOn ?? false;

        public override void Dispose()
        {
            if (_isTwoWay && _toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
            base.Dispose();
        }

        private void OnToggleChanged(bool uiValue)
        {
            if (_property != null)
            {
                object finalValue = Options.Converter != null ? Options.Converter.ConvertBack(uiValue) : uiValue;
                _property.Value = (bool)finalValue;
            }
        }

        public override void UpdateToProperty()
        {
            if(_toggle != null) OnToggleChanged(_toggle.isOn);
        }
    }
}