using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binding for Image sprite
    /// </summary>
    public class SpriteBinding : UIBinding<Sprite>
    {
        private readonly Image _imageComponent;

        public SpriteBinding(string propertyKey, Image imageComponent) : base(propertyKey, imageComponent)
        {
            _imageComponent = imageComponent;
        }

        public override void UpdateUI(Sprite value)
        {
            if (_imageComponent != null)
            {
                _imageComponent.sprite = value;
            }
        }

        public override Sprite GetUIValue()
        {
            return _imageComponent?.sprite;
        }
    }
}
