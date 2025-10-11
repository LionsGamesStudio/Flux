using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a boolean reactive property to a Unity Toggle component.
    /// Follows the passive/activable pattern for architectural consistency and robustness.
    /// </summary>
    [BindingFor(typeof(Toggle))]
    public class ToggleBinding : UIBinding<bool>
    {
        private readonly Toggle _toggle;
        private IReactiveProperty<bool> _property;

        public ToggleBinding(string propertyKey, Toggle toggle) 
            : base(propertyKey, toggle)
        {
            _toggle = toggle;
        }

        /// <summary>
        /// This method is called by the ReactiveBindingSystem once it has the definitive
        /// ReactiveProperty instance. This is where we activate the binding logic.
        /// </summary>
        public override void Activate(ReactiveProperty<bool> property)
        {
            if (property == null) return;
            _property = property;

            // Activate TwoWay binding if required by the options
            if (Options.Mode == Attributes.BindingMode.TwoWay || Options.Mode == Attributes.BindingMode.OneWayToSource)
            {
                _toggle?.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        public override void UpdateUI(bool value)
        {
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(value);
            }
        }

        public override bool GetUIValue() => _toggle?.isOn ?? false;

        public override void Dispose()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
            base.Dispose();
        }

        private void OnToggleChanged(bool uiValue)
        {
            if (_property != null && IsActive)
            {
                _property.Value = uiValue;
            }
        }

        public override void UpdateToProperty()
        {
            if(_toggle != null)
            {
                OnToggleChanged(_toggle.isOn);
            }
        }
    }
}