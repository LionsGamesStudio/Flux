using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binding for Image sprite.
    /// </summary>
    [BindingFor(typeof(Image))]
    public class SpriteBinding : UIBinding<Sprite>
    {
        private readonly Image _imageComponent;
        private IReactiveProperty<Sprite> _property;

        public SpriteBinding(string propertyKey, Image imageComponent) : base(propertyKey, imageComponent)
        {
            _imageComponent = imageComponent;
        }
        
        /// <summary>
        /// Activates the binding by receiving the definitive property instance.
        /// </summary>
        public override void Activate(ReactiveProperty<Sprite> property)
        {
            _property = property;
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