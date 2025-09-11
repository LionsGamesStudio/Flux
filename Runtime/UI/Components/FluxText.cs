using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FluxFramework.Attributes;
using FluxFramework.Binding;

namespace FluxFramework.UI
{
    /// <summary>
    /// A UI component that provides reactive binding for a TextMeshPro or legacy Text component.
    /// Binding is handled automatically by the base class.
    /// </summary>
    public class FluxText : FluxUIComponent
    {
        [Header("Component References")]
        [Tooltip("The TextMeshProUGUI component. Has priority over the legacy Text component.")]
        [SerializeField] private TextMeshProUGUI textComponent;
        [Tooltip("The legacy UI Text component. Used if no TextMeshPro component is found.")]
        [SerializeField] private Text legacyTextComponent;
        
        [Header("Display Options")]
        [Tooltip("An optional format string for the bound value (e.g., 'Score: {0}').")]
        [SerializeField] private string formatString = "{0}";

        // --- Declarative Binding ---
        [Header("Binding Configuration")]
        [Tooltip("Assign the Text or TextMeshProUGUI component here to configure its data binding.")]
        [FluxBinding("ui.text.value")]
        [SerializeField] private Component _textBindingTarget; // Use base 'Component' for flexibility

        protected override void InitializeComponent()
        {
            if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
            if (legacyTextComponent == null && textComponent == null) legacyTextComponent = GetComponent<Text>();

            if (_textBindingTarget == null)
            {
                _textBindingTarget = textComponent != null ? (Component)textComponent : legacyTextComponent;
            }
        }
        
        #region Public API
        
        public string GetCurrentText()
        {
            if (textComponent != null) return textComponent.text;
            if (legacyTextComponent != null) return legacyTextComponent.text;
            return "";
        }
        
        public virtual void SetText(string newText)
        {
            if (textComponent != null) textComponent.text = newText;
            else if (legacyTextComponent != null) legacyTextComponent.text = newText;
        }
        
        #endregion
    }
}