namespace FluxFramework.Core
{
    /// <summary>
    /// A static access point for the core Flux Framework manager.
    /// This is the preferred way to interact with the framework.
    /// </summary>
    public static class Flux
    {
        /// <summary>
        /// Provides access to the core framework manager through its interface.
        /// All framework interactions should go through this property.
        /// </summary>
        public static IFluxManager Manager => FluxManager.Instance;

        /// <summary>
        /// A global event that is fired once the Flux Framework is fully initialized.
        /// Components should subscribe to this to perform setup logic safely.
        /// </summary>
        public static event Action OnFrameworkInitialized;
        
        /// <summary>
        /// Internal method for the FluxManager to invoke the initialization event.
        /// This ensures only the framework itself can trigger this global event.
        /// </summary>
        internal static void InvokeOnFrameworkInitialized()
        {
            OnFrameworkInitialized?.Invoke();
        }
    }
}