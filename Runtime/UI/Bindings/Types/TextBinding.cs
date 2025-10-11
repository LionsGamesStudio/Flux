using TMPro;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a property to a TextMeshProUGUI component's text field.
    /// Follows the passive/activable pattern for architectural consistency.
    /// </summary>
    [BindingFor(typeof(TextMeshProUGUI))]
    public class TextBinding : UIBinding<string>
    {
        private readonly TextMeshProUGUI _textComponent;
        private readonly string _formatString;
        private IReactiveProperty<string> _property;

        /// <summary>
        /// This constructor is called by the BindingFactory.
        /// It provides a default format string and chains to the main constructor.
        /// </summary>
        public TextBinding(string propertyKey, TextMeshProUGUI textComponent) 
            : this(propertyKey, textComponent, "{0}")
        {
            // This constructor's body is empty because all the work is done
            // by the main constructor it calls.
        }

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