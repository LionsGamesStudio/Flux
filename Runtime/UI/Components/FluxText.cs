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
        
        /// <summary>
        /// Applies the global theme's font and color to this text component.
        /// </summary>
        public override void ApplyTheme()
        {
            base.ApplyTheme();
            
            var theme = UIThemeManager.CurrentTheme;
            if (theme == null) return;

            // Apply to TextMeshPro component if it exists
            if (textComponent != null)
            {
                // Apply font if one is assigned in the theme
                if (theme.primaryFont_TMP != null)
                {
                    textComponent.font = theme.primaryFont_TMP;
                }
                // Apply font size
                textComponent.fontSize = theme.defaultFontSize;
                // Apply color
                textComponent.color = theme.textColor;
            }
            // Apply to legacy Text component if it exists
            else if (legacyTextComponent != null)
            {
                // Apply font if one is assigned in the theme
                if (theme.primaryFont_Legacy != null)
                {
                    legacyTextComponent.font = theme.primaryFont_Legacy;
                }
                // Apply font size
                legacyTextComponent.fontSize = theme.defaultFontSize;
                // Apply color
                legacyTextComponent.color = theme.textColor;
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