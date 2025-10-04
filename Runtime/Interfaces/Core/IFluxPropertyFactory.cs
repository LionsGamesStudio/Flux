using System.Collections.Generic;

namespace FluxFramework.Core
{
    /// <summary>
    /// Defines the contract for the service responsible for creating and registering reactive properties.
    /// </summary>
    public interface IFluxPropertyFactory
    {
        /// <summary>
        /// Scans the target object for fields marked with [ReactiveProperty] and registers them.
        /// </summary>
        /// <param name="owner"></param>
        /// <returns>A collection of property names that were registered.</returns>
        IEnumerable<string> RegisterPropertiesFor(object owner);
    }
}