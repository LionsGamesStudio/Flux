using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

namespace FluxFramework.UI
{
    /// <summary>
    /// A UI component that provides reactive binding for a Unity Toggle.
    /// Binding is handled automatically by the base class.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class FluxToggle : FluxUIComponent
    {
        [Header("Component References")]
        [Tooltip("The Unity Toggle component. If null, it will be found automatically.")]
        [SerializeField] private Toggle toggleComponent;

        // --- Declarative Binding ---
        [Header("Binding Configuration")]
        [Tooltip("Assign the Toggle component here to configure its data binding.")]
        [FluxBinding("ui.toggle.value", Mode = BindingMode.TwoWay)]
        [SerializeField] private Toggle _valueBindingTarget;

        protected override void InitializeComponent()
        {
            if (toggleComponent == null)
            {
                toggleComponent = GetComponent<Toggle>();
            }
            if (_valueBindingTarget == null)
            {
                _valueBindingTarget = toggleComponent;
            }
        }
        
        #region Public API
        public bool GetCurrentValue() => toggleComponent?.isOn ?? false;
        public void SetValueWithoutNotify(bool value) { toggleComponent?.SetIsOnWithoutNotify(value); }
        #endregion
    }
}