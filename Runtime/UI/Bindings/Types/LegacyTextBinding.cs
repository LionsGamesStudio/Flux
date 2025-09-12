using UnityEngine.UI;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a string property to a legacy UnityEngine.UI.Text component's text field.
    /// </summary>
    public class LegacyTextBinding : UIBinding<string>
    {
        private readonly Text _textComponent;
        private readonly string _formatString;

        public LegacyTextBinding(string propertyKey, Text textComponent, string formatString = "{0}") 
            : base(propertyKey, textComponent)
        {
            _textComponent = textComponent;
            _formatString = formatString;
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