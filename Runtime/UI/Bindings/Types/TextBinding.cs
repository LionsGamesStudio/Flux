using TMPro;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a string property to a TextMeshProUGUI component's text field.
    /// </summary>
    public class TextBinding : UIBinding<string>
    {
        private readonly TextMeshProUGUI _textComponent;

        public TextBinding(string propertyKey, TextMeshProUGUI textComponent) 
            : base(propertyKey, textComponent)
        {
            _textComponent = textComponent;
        }

        public override void UpdateUI(string value)
        {
            if (_textComponent != null)
            {
                _textComponent.text = value ?? "";
            }
        }

        public override string GetUIValue()
        {
            return _textComponent?.text ?? "";
        }
    }
}