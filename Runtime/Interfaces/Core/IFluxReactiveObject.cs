namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for objects that have reactive properties that can be initialized by the framework.
    /// </summary>
    public interface IFluxReactiveObject
    {
        void InitializeReactiveProperties(IFluxManager manager);
    }
}