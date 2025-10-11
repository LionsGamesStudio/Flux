using UnityEngine;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Defines a contract for a factory that creates UI binding instances for specific component types.
    /// </summary>
    public interface IBindingFactory
    {
        /// <summary>
        /// Creates an appropriate IUIBinding instance for the given UI component.
        /// </summary>
        /// <param name="propertyKey">The property key the binding will be associated with.</param>
        /// <param name="uiComponent">The UI component instance to be bound.</param>
        /// <returns>An new instance of a suitable IUIBinding, or null if no creator is registered for the component type.</returns>
        IUIBinding Create(string propertyKey, Component uiComponent);
    }
}