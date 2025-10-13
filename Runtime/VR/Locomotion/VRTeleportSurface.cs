#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.VR
{
    /// <summary>
    /// Marks a surface as valid for teleportation.
    /// Attach this to any floor or platform where the player should be able to teleport.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VRTeleportSurface : FluxMonoBehaviour
    {
        [Header("Surface Settings")]
        [Tooltip("If false, this surface will not be considered valid for teleportation.")]
        [SerializeField] private bool isEnabled = true;
        
        [Tooltip("Optional: Custom color for the teleport marker on this surface.")]
        [SerializeField] private Color surfaceMarkerColor = Color.green;
        
        [Tooltip("If true, the player's Y position will snap to the surface. If false, only XZ position changes.")]
        [SerializeField] private bool snapToSurfaceHeight = true;
        
        [Header("Visual Feedback")]
        [Tooltip("Optional material to highlight the surface when it's being targeted for teleportation.")]
        [SerializeField] private Material highlightMaterial;
        
        private Renderer _renderer;
        private Material _originalMaterial;
        private bool _isBeingTargeted;
        
        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();
            
            // Ensure the collider is not a trigger for proper physics raycast
            var collider = GetComponent<Collider>();
            if (collider.isTrigger)
            {
                Flux.Manager.Logger.Warning($"VRTeleportSurface on {gameObject.name} has a trigger collider. This may cause teleportation issues.", this);
            }
            
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _originalMaterial = _renderer.material;
            }
        }
        
        /// <summary>
        /// Checks if this surface is currently enabled for teleportation.
        /// </summary>
        public bool IsEnabled => isEnabled;
        
        /// <summary>
        /// Gets the custom marker color for this surface.
        /// </summary>
        public Color MarkerColor => surfaceMarkerColor;
        
        /// <summary>
        /// Gets whether the teleport should snap to this surface's height.
        /// </summary>
        public bool SnapToHeight => snapToSurfaceHeight;
        
        /// <summary>
        /// Enables or disables this teleport surface at runtime.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled && _isBeingTargeted)
            {
                OnTargetExit();
            }
        }
        
        /// <summary>
        /// Called when the teleport ray is targeting this surface.
        /// </summary>
        public void OnTargetEnter()
        {
            if (!isEnabled) return;
            
            _isBeingTargeted = true;
            
            if (_renderer != null && highlightMaterial != null)
            {
                _renderer.material = highlightMaterial;
            }
        }
        
        /// <summary>
        /// Called when the teleport ray stops targeting this surface.
        /// </summary>
        public void OnTargetExit()
        {
            _isBeingTargeted = false;
            
            if (_renderer != null && _originalMaterial != null)
            {
                _renderer.material = _originalMaterial;
            }
        }
        
        protected override void OnFluxDestroy()
        {
            base.OnFluxDestroy();
            
            // Clean up highlight if surface is destroyed while being targeted
            if (_isBeingTargeted)
            {
                OnTargetExit();
            }
        }
        
        private void OnDisable()
        {
            if (_isBeingTargeted)
            {
                OnTargetExit();
            }
        }
    }
}
#endif