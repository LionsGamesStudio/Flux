using FluxFramework.Core;

namespace FluxFramework.Binding
{
    public interface IReactiveBindingSystem
    {
        /// <summary>
        /// Initializes the Reactive Binding System. This should be called once during application startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Binds a UI binding to a reactive property by key with optional binding options.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyKey"></param>
        /// <param name="binding"></param>
        /// <param name="options"></param>
        void Bind<T>(string propertyKey, IUIBinding<T> binding, BindingOptions options = null);
        
        /// <summary>
        /// Unbinds a specific UI binding from a reactive property by key.
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <param name="binding"></param>
        void Unbind(string propertyKey, IUIBinding binding);

        /// <summary>
        /// Unbinds all UI bindings from a reactive property by key.
        /// </summary>
        /// <param name="propertyKey"></param>
        void Unbind(string propertyKey);

        /// <summary>
        /// Clears all bindings from the system.
        /// </summary>
        void ClearAll();
        
        /// <summary>
        /// Refreshes all bindings associated with the specified property key.
        /// </summary>
        /// <param name="propertyKey"></param>
        void RefreshBindings(string propertyKey);

        /// <summary>
        /// Gets the total number of active bindings in the system.
        /// </summary>
        /// <returns></returns>
        int GetActiveBindingCount();
    }
}