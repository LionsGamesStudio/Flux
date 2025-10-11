#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Locomotion;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;

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

            // --- Otherwise, build the rig from scratch ---

            // 1. Create the main player GameObject
            GameObject vrRig = new GameObject("FluxVR Player");
            vrRig.transform.position = Vector3.zero;
            vrRig.AddComponent<CharacterController>();
            
            // 2. HMD Camera Setup
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(vrRig.transform);
            cameraOffset.transform.localPosition = Vector3.zero;
            
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(cameraOffset.transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.7f, 0); // Average eye height
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();

            var trackedPoseDriver = cameraGO.AddComponent<TrackedPoseDriver>();
            trackedPoseDriver.SetPoseDataSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            
            // 3. Add Locomotion and Manager components
            vrRig.AddComponent<FluxVRManager>();
            vrRig.AddComponent<FluxVRLocomotion>();
            vrRig.AddComponent<FluxVRPlayer>();

            // 4. Optionally, instantiate visuals for controllers and HMD
            CreateControllerVisuals(vrRig.transform, locomotion);

            Debug.Log("[FluxFramework] A new FluxVR Player Rig has been created programmatically.", this);
            _createdPlayer = vrRig;
        }

        /// <summary>
        /// Creates the visual GameObjects for controllers and teleportation,
        /// and connects them to the locomotion system.
        /// </summary>
        private void CreateControllerVisuals(Transform rigParent, FluxVRLocomotion locomotion)
        {            
            // Create Line Renderer for Teleportation
            GameObject teleportLineGO = new GameObject("Teleport Line");
            teleportLineGO.transform.SetParent(rigParent, false); // Attaché au rig
            var lineRenderer = teleportLineGO.AddComponent<LineRenderer>();
            
            // Configuration of LineRenderer
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.white;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            
            // Assign the LineRenderer to the locomotion system
            locomotion.teleportLineRenderer = lineRenderer;
            
            // Create a simple Teleport Marker
            GameObject teleportMarkerGO = new GameObject("Teleport Marker");

            // Create a simple visual, like a flattened cylinder
            GameObject markerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerVisual.transform.SetParent(teleportMarkerGO.transform, false);
            markerVisual.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            Destroy(markerVisual.GetComponent<Collider>());
            
            // Assign the Teleport Marker to the locomotion system
            locomotion.teleportMarkerPrefab = teleportMarkerGO;
            teleportMarkerGO.SetActive(false);
        }
    }
}
#endif