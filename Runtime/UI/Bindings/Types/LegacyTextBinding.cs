using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a property to a legacy UnityEngine.UI.Text component's text field.
    /// Follows the passive/activable pattern for architectural consistency.
    /// </summary>
    public class LegacyTextBinding : UIBinding<string>
    {
        private readonly Text _textComponent;
        private readonly string _formatString;
        private IReactiveProperty<string> _property;

        public LegacyTextBinding(string propertyKey, Text textComponent, string formatString = "{0}") 
            : base(propertyKey, textComponent)
        {
            _textComponent = textComponent;
            _formatString = string.IsNullOrEmpty(formatString) ? "{0}" : formatString;
        }

        /// <summary>
        /// Activates the binding by receiving the definitive property instance from the system.
        /// </summary>
        public override void Activate(ReactiveProperty<string> property)
        {
            _property = property;
        }

        public override void UpdateUI(string value)
        {
            if (_textComponent != null)
            {
                _textComponent.text = string.Format(_formatString, value ?? "");
            }
        }

        public override string GetUIValue()
        {
            return _textComponent?.text ?? "";
        }
    }
}