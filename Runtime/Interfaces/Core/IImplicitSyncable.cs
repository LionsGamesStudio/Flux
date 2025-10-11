using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// Defines a contract for reactive properties that can synchronize their internal state
    /// with a local field on a MonoBehaviour. This is used by the implicit pattern.
    /// </summary>
    public interface IImplicitSyncable
    {
        /// <summary>
        /// Sets up the necessary event subscriptions to keep a local field in sync with this reactive property.
        /// </summary>
        /// <param name="owner">The MonoBehaviour instance that owns the local field.</param>
        /// <param name="localFieldInstance">The actual instance of the local collection (e.g., the List<T> or Dictionary<,> field).</param>
        void SetupImplicitSync(MonoBehaviour owner, object localFieldInstance);
    }
}