#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Locomotion;
using UnityEngine.XR;

namespace FluxFramework.VR
{
    /// <summary>
    /// A factory/builder component that can programmatically create a complete,
    /// fully-functional VR Player Rig with all necessary FluxVR components.
    /// </summary>
    public class FluxVRPlayerPrefab : FluxMonoBehaviour
    {
        [Header("VR Player Configuration")]
        [Tooltip("If true, a VR Player Rig will be created automatically at the start of the scene.")]
        [SerializeField] private bool createAtStart = true;
        
        [Header("Custom Prefab References")]
        [Tooltip("Optional: Assign a complete VR player prefab. If assigned, this will be instantiated instead of building one from scratch.")]
        [SerializeField] private GameObject vrPlayerPrefab;

        [Tooltip("Optional: A prefab for the Camera Rig object. Used if building from scratch.")]
        [SerializeField] private GameObject cameraRigPrefab;

        private GameObject _createdPlayer;

        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();
            if (createAtStart && _createdPlayer == null)
            {
                CreateVRPlayerPrefab();
            }
        }
        
        /// <summary>
        /// Creates or instantiates a complete VR Player prefab.
        /// </summary>
        [FluxButton("Create VR Player")]
        public void CreateVRPlayerPrefab()
        {
            // If a full prefab is assigned, instantiate it.
            if (vrPlayerPrefab != null)
            {
                _createdPlayer = Instantiate(vrPlayerPrefab, transform.position, transform.rotation);
            }

            // --- Otherwise, build the rig from scratch (original logic) ---

            GameObject vrRig = new GameObject("FluxVR Player");
            vrRig.transform.position = Vector3.zero;
            
            vrRig.AddComponent<CharacterController>();
            
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(vrRig.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(cameraOffset.transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.7f, 0); // Average eye height
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            
            vrRig.AddComponent<FluxVRManager>();
            vrRig.AddComponent<FluxVRLocomotion>();
            vrRig.AddComponent<FluxVRPlayer>();

            FluxFramework.Core.Flux.Manager.Logger.Info("[FluxFramework] A new FluxVR Player Rig has been created programmatically.", this);
            _createdPlayer = vrRig;
        }
    }
}
#endif