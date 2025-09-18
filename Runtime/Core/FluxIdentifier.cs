using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// Attaches a unique, persistent identifier to a GameObject.
    /// This is used by the persistence system to find and reference specific scene objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class FluxIdentifier : MonoBehaviour
    {
        [Tooltip("The unique ID for this object. Should not be changed manually.")]
        [SerializeField]
        private string _id;
        public string Id => _id;

        // Automatically assign a new GUID when the component is added in the editor.
        private void Reset()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
            }
        }
    }
}