using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.Binding;

namespace FluxFramework.UI
{
    /// <summary>
    /// A generic, reusable UI component that can bind an Image's sprite or color to a reactive property.
    /// The property keys are configured directly in the inspector.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FluxImage : FluxUIComponent
    {
        [Header("Component Reference")]
        [Tooltip("The Unity Image component to control. If null, it will be found automatically.")]
        [SerializeField] private Image imageComponent;

        [Header("Binding Configuration")]
        [Tooltip("(Optional) The Reactive Property Key to bind this image's SPRITE to.")]
        [SerializeField] private string _spritePropertyKey;
        
        [Tooltip("(Optional) The Reactive Property Key to bind this image's COLOR to.")]
        [SerializeField] private string _colorPropertyKey;

        // --- Private Binding References ---
        private IUIBinding _spriteBinding;
        private IUIBinding _colorBinding;

        /// <summary>
        /// Gets the reference to the Image component.
        /// </summary>
        protected override void InitializeComponent()
        {
            if (imageComponent == null)
            {
                imageComponent = GetComponent<Image>();
            }
        }
        
        /// <summary>
        /// Manually creates the bindings for sprite and/or color based on the configured property keys.
        /// </summary>
        protected override void RegisterCustomBindings()
        {
            if (imageComponent == null) return;
            
            // --- Create Sprite Binding if key is provided ---
            if (!string.IsNullOrEmpty(_spritePropertyKey))
            {
                _spriteBinding = new SpriteBinding(_spritePropertyKey, imageComponent);
                ReactiveBindingSystem.Bind(_spritePropertyKey, _spriteBinding, new BindingOptions());
                TrackBinding(_spriteBinding); // Track for automatic cleanup
            }

            // --- Create Color Binding if key is provided ---
            if (!string.IsNullOrEmpty(_colorPropertyKey))
            {
                _colorBinding = new ColorBinding(_colorPropertyKey, imageComponent);
                ReactiveBindingSystem.Bind(_colorPropertyKey, _colorBinding, new BindingOptions());
                TrackBinding(_colorBinding); // Track for automatic cleanup
            }
        }
        
        /// <summary>
        /// Applies the global theme to this component. Can be used to set a default or background color.
        /// </summary>
        public override void ApplyTheme()
        {
            base.ApplyTheme();
            
            var theme = UIThemeManager.CurrentTheme;
            if (theme == null || imageComponent == null) return;

            if (string.IsNullOrEmpty(_colorPropertyKey))
            {
                imageComponent.color = theme.backgroundColor;
            }
        }
        
        #region Public API
        
        public Sprite GetCurrentSprite() => imageComponent?.sprite;
        public Color GetCurrentColor() => imageComponent?.color ?? Color.white;
        
        #endregion
    }
}