using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Attributes;

namespace FluxFramework.UI
{
    /// <summary>
    /// A UI component that provides reactive bindings for an Image's sprite and color properties.
    /// Binding is handled automatically by the base FluxUIComponent class via [FluxBinding] attributes.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FluxImage : FluxUIComponent
    {
        [Header("Component References")]
        [Tooltip("The Unity Image component to bind to. If null, it will be found automatically.")]
        [SerializeField] private Image imageComponent;
        
        // --- Declarative Bindings ---
        // The base class will automatically find these attributes and create the appropriate bindings.

        [Header("Binding Configuration")]
        [Tooltip("Assign the Image component here. The binding system will link its 'sprite' property.")]
        [FluxBinding("ui.image.sprite")]
        [SerializeField] private Image _spriteBindingTarget;

        [Tooltip("Assign the Image component here. The binding system will link its 'color' property.")]
        [FluxBinding("ui.image.color")]
        [SerializeField] private Image _colorBindingTarget;

        /// <summary>
        /// This method is now used for all component-specific setup.
        /// It's called automatically by the base class at the correct time.
        /// </summary>
        protected override void InitializeComponent()
        {
            if (imageComponent == null)
            {
                imageComponent = GetComponent<Image>();
            }
            
            if (_spriteBindingTarget == null) _spriteBindingTarget = imageComponent;
            if (_colorBindingTarget == null) _colorBindingTarget = imageComponent;
        }

        protected override void ApplyTheme()
        {
            base.ApplyTheme();
            
            var theme = UIThemeManager.CurrentTheme;
            if (theme == null) return;

            // Apply background color from theme
            if (imageComponent != null)
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