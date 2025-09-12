using TMPro;
using UnityEngine;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binds a string property to a TextMeshProUGUI component's text field.
    /// </summary>
    public class TextBinding : UIBinding<string>
    {
        private readonly TextMeshProUGUI _textComponent;
        private readonly string _formatString;

        public TextBinding(string propertyKey, TextMeshProUGUI textComponent, string formatString = "{0}") 
            : base(propertyKey, textComponent)
        {
            _textComponent = textComponent;
            _formatString = formatString;
        }

        public override void UpdateUI(string value)
        {
            Debug.Log($"[TextBinding] Received new value to display: '{value}'");
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