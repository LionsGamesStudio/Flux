namespace FluxFramework.Core
{
    public interface IFluxPersistenceManager
    {
        /// <summary>
        /// Registers a property as persistent, ensuring its value is saved and restored across sessions.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="property"></param>
        void RegisterPersistentProperty(string key, IReactiveProperty property);

        /// <summary>
        /// Loads all registered persistent properties from storage.
        /// </summary>
        void LoadAllRegisteredProperties();

        /// <summary>
        /// Saves all registered persistent properties to storage.
        /// </summary>
        void SaveAll();

        /// <summary>
        /// Loads all registered persistent properties from storage.
        /// </summary>
        void Shutdown();
    }
}