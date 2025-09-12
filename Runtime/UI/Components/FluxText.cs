using UnityEngine;
using TMPro;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.UI;

namespace FluxFramework.UI
{
    /// <summary>
    /// A generic, reusable UI component that binds to and displays a string property.
    /// The property key is configured directly in the inspector.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FluxText : FluxUIComponent
    {
        [Header("Binding Configuration")]
        [Tooltip("The Reactive Property Key to bind this text to.")]
        [SerializeField] private string _propertyKey;
        
        [Tooltip("An optional format string for the value (e.g., 'Score: {0}').")]
        [SerializeField] private string _formatString = "{0}";

        [Header("Component Reference")]
        [SerializeField] private TextMeshProUGUI _textComponent;

        private TextBinding _binding;

        /// <summary>
        /// Gets the reference to the TextMeshProUGUI component.
        /// </summary>
        protected override void InitializeComponent()
        {
            if (_textComponent == null) _textComponent = GetComponent<TextMeshProUGUI>();
        }
        
        /// <summary>
        /// This component uses a manual binding approach for maximum flexibility.
        /// It overrides the custom binding method to create its binding explicitly.
        /// </summary>
        protected override void RegisterCustomBindings()
        {
            // The automatic attribute-based binding is skipped because this class doesn't use [FluxBinding].
            // We create our binding manually here.

            if (string.IsNullOrEmpty(_propertyKey) || _textComponent == null) return;
            
            // We create a new TextBinding instance.
            _binding = new TextBinding(_propertyKey, _textComponent, _formatString);
            
            // Register it with the central system.
            ReactiveBindingSystem.Bind(_propertyKey, _binding, new BindingOptions());
            
            // Track the binding for automatic cleanup.
            TrackBinding(_binding);
        }
    }
}