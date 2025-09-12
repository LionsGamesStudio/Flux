using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a boolean reactive property to a Unity Toggle component.
    /// Supports both one-way and two-way data binding and honors value converters.
    /// </summary>
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
                _property = FluxManager.Instance.GetOrCreateProperty<bool>(propertyKey, default);
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        /// <summary>
        /// Updates the Toggle's UI state from the property's value.
        /// </summary>
        public override void UpdateUI(bool value)
        {
            if (_toggle != null)
            {
                // Use SetIsOnWithoutNotify to prevent firing the onValueChanged event,
                // which would cause an infinite loop in a two-way binding.
                _toggle.SetIsOnWithoutNotify(value);
            }
        }

        /// <summary>
        /// Gets the current boolean state from the UI Toggle.
        /// </summary>
        public override bool GetUIValue() => _toggle?.isOn ?? false;

        /// <summary>
        /// Cleans up the event listener when the binding is disposed.
        /// </summary>
        public override void Dispose()
        {
            // The check for _isTwoWay ensures we only try to remove a listener that was actually added.
            if (_isTwoWay && _toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
            base.Dispose();
        }

        /// <summary>
        /// Called when the user clicks the UI Toggle. Updates the source reactive property.
        /// </summary>
        private void OnToggleChanged(bool uiValue)
        {
            if (_property != null)
            {
                // If a value converter is provided in the options, use it to convert the value back
                // before updating the data model.
                object finalValue = Options.Converter != null ? Options.Converter.ConvertBack(uiValue) : uiValue;
                _property.Value = (bool)finalValue;
            }
        }

        /// <summary>
        /// Manually pushes the UI's current value to the source property.
        /// </summary>
        public override void UpdateToProperty()
        {
            if(_toggle != null)
            {
                OnToggleChanged(_toggle.isOn);
            }
        }
    }
}