using TMPro;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a property to a TextMeshProUGUI component's text field.
    /// Follows the passive/activable pattern for architectural consistency.
    /// </summary>
    public class TextBinding : UIBinding<string>
    {
        private readonly TextMeshProUGUI _textComponent;
        private readonly string _formatString;
        private IReactiveProperty<string> _property;

        public TextBinding(string propertyKey, TextMeshProUGUI textComponent, string formatString = "{0}") 
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
                // We use the raw value because any type conversion (e.g., int to string)
                // has already been handled by the ReactiveBindingSystem via a converter.
                _textComponent.text = string.Format(_formatString, value ?? "");
            }
        }

        public override string GetUIValue()
        {
            return _textComponent?.text ?? "";
        }
    }
}