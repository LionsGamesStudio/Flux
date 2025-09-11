using UnityEngine.UI;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a string property to a legacy UnityEngine.UI.Text component's text field.
    /// </summary>
    public class LegacyTextBinding : UIBinding<string>
    {
        private readonly Text _textComponent;

        public LegacyTextBinding(string propertyKey, Text textComponent) 
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