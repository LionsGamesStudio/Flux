using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binding for Image color
    /// </summary>
    public class ColorBinding : UIBinding<Color>
    {
        private readonly Image _imageComponent;

        public ColorBinding(string propertyKey, Image imageComponent) : base(propertyKey, imageComponent)
        {
            _imageComponent = imageComponent;
        }

        public override void UpdateUI(Color value)
        {
            if (_imageComponent != null)
            {
                _imageComponent.color = value;
            }
        }

        public override Color GetUIValue()
        {
            return _imageComponent?.color ?? Color.white;
        }
    }
}
