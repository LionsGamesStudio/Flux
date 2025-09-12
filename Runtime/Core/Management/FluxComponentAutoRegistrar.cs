using UnityEngine;
using System.Collections;

namespace FluxFramework.Core
{
    /// <summary>
    /// Monitors the scene for new FluxComponent instances and automatically registers them
    /// </summary>
    [System.Serializable]
    public class FluxComponentAutoRegistrar : MonoBehaviour
    {
        [SerializeField] private float scanInterval = 1.0f; // Scan every second
        [SerializeField] private bool enableRuntimeScanning = true;
        
        private Coroutine _scanCoroutine;
        
        private void Start()
        {
            if (enableRuntimeScanning)
            {
                StartRuntimeScanning();
            }
        }
        
        private void OnDestroy()
        {
            StopRuntimeScanning();
        }
        
        /// <summary>
        /// Starts periodic scanning for new FluxComponents
        /// </summary>
        public void StartRuntimeScanning()
        {
            if (_scanCoroutine == null)
            {
                _scanCoroutine = StartCoroutine(ScanForNewComponents());
                Debug.Log("[FluxFramework] Started runtime component scanning");
            }
        }
        
        /// <summary>
        /// Stops periodic scanning
        /// </summary>
        public void StopRuntimeScanning()
        {
            if (_scanCoroutine != null)
            {
                StopCoroutine(_scanCoroutine);
                _scanCoroutine = null;
                Debug.Log("[FluxFramework] Stopped runtime component scanning");
            }
        }
        
        /// <summary>
        /// Performs an immediate scan for new components
        /// </summary>
        public void ScanNow()
        {
            FluxComponentRegistry.RegisterAllComponentsInScene();
        }
        
        private IEnumerator ScanForNewComponents()
        {
            while (true)
            {
                yield return new WaitForSeconds(scanInterval);
                
                if (Flux.Manager != null)
                {
                    FluxComponentRegistry.RegisterAllComponentsInScene();
                }
            }
        }
        
        /// <summary>
        /// Called when a new GameObject is instantiated (Unity event)
        /// </summary>
        private void OnSceneObjectAdded()
        {
            // This would be called by Unity when objects are added to scene
            // For now, we rely on periodic scanning
            if (enableRuntimeScanning)
            {
                ScanNow();
            }
        }
    }
}
