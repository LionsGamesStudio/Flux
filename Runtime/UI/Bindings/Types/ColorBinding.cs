using UnityEngine;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Binding for Image color.
    /// </summary>
    [BindingFor(typeof(Image))]
    public class ColorBinding : UIBinding<Color>
    {
        private readonly Image _imageComponent;
        private IReactiveProperty<Color> _property;

        public ColorBinding(string propertyKey, Image imageComponent) : base(propertyKey, imageComponent)
        {
            _imageComponent = imageComponent;
        }

        /// <summary>
        /// Activates the binding by receiving the definitive property instance.
        /// </summary>
        public override void Activate(ReactiveProperty<Color> property)
        {
            _property = property;
            // For a OneWay binding, there are no listeners to attach, but the pattern is respected.
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