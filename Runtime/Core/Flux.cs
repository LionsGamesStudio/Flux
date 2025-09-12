namespace FluxFramework.Core
{
    /// <summary>
    /// A static access point and cache for core Flux Framework services.
    /// This class provides direct access to framework managers via their interfaces,
    /// decoupling consumer code from the concrete MonoBehaviour implementations.
    /// </summary>
    public static class Flux
    {
        /// <summary> Provides access to the core framework manager. </summary>
        public static IFluxManager Manager { get; internal set; }

        /// <summary> Provides access to the property management service. </summary>
        public static IFluxPropertyManager Properties { get; internal set; }
        
        /// <summary> Provides access to the thread management service. </summary>
        public static IFluxThreadManager Threading { get; internal set; }
        
#if UNITY_EDITOR
        /// <summary>
        // Allows test assemblies to replace the live services with mock implementations.
        /// </summary>
        internal static void SetServicesForTesting(IFluxManager manager, IFluxPropertyManager properties, IFluxThreadManager threading)
        {
            Manager = manager;
            Properties = properties;
            Threading = threading;
        }
#endif
    }
}