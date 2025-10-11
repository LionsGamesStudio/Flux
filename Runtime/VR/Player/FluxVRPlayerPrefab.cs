    
#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Locomotion;

namespace FluxFramework.VR
{
    /// <summary>
    /// A factory/builder component that can programmatically create a complete,
    /// fully-functional VR Player Rig with all necessary FluxVR components and visuals.
    /// </summary>
    public class FluxVRPlayerPrefab : FluxMonoBehaviour
    {
        [Header("VR Player Configuration")]
        [Tooltip("If true, a VR Player Rig will be created automatically when the scene starts.")]
        [SerializeField] private bool createAtStart = true;
        
        [Header("Custom Prefab References")]
        [Tooltip("Optional: Assign a complete VR player prefab. If assigned, this will be instantiated instead of building one from scratch.")]
        [SerializeField] private GameObject vrPlayerPrefab;

        private GameObject _createdPlayer;

        /// <summary>
        /// OnFluxAwake is called after the framework is initialized.
        /// It triggers the automatic creation of the VR rig if configured to do so.
        /// </summary>
        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();
            if (createAtStart && _createdPlayer == null)
            {
                CreateVRPlayerPrefab();
            }
        }
        
        /// <summary>
        /// Creates or instantiates a complete and visually functional VR Player rig.
        /// This method can be called at runtime or from the editor via a [FluxButton].
        /// </summary>
        [FluxButton("Create VR Player")]
        public void CreateVRPlayerPrefab()
        {
            if (_createdPlayer != null)
            {
                Flux.Manager.Logger.Warning("A VR Player instance created by this component already exists. It will be destroyed before creating a new one.", this);
                DestroyPlayerInstance();
            }

            // If a full prefab is assigned, instantiate it and exit.
            if (vrPlayerPrefab != null)
            {
                _createdPlayer = Instantiate(vrPlayerPrefab, transform.position, transform.rotation);
                Flux.Manager.Logger.Info("Instantiated VR Player from the provided prefab.", this);
                return;
            }

            // --- Otherwise, build the rig programmatically from scratch ---

            // 1. Create the main player Rig GameObject
            GameObject vrRig = new GameObject("FluxVR Player");
            vrRig.transform.position = Vector3.zero;
            vrRig.AddComponent<CharacterController>();
            
            // 2. HMD Camera Setup
            // The Camera is placed directly under the Rig. The CustomTrackedPoseDriver handles its local position.
            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(vrRig.transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.7f, 0); // Set an average eye height as a starting point.
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();

            var poseDriver = cameraGO.AddComponent<CustomTrackedPoseDriver>();
            poseDriver.poseToTrack = CustomTrackedPoseDriver.TrackedPose.Center;
            poseDriver.trackingType = CustomTrackedPoseDriver.TrackingType.RotationAndPosition;
            // It's best practice to update camera tracking just before rendering to minimize perceived latency.
            poseDriver.updateInUpdate = false;
            poseDriver.updateInBeforeRender = true;
            
            // 3. Add Core Flux VR Logic Components
            var vrManager = vrRig.AddComponent<FluxVRManager>();
            var locomotion = vrRig.AddComponent<FluxVRLocomotion>();
            vrRig.AddComponent<FluxVRPlayer>();

            // 4. Create and connect the visual elements needed by the locomotion system.
            CreateLocomotionVisuals(vrRig.transform, locomotion);
            
            _createdPlayer = vrRig;
            Debug.Log("A new FluxVR Player Rig has been created programmatically.");
        }

        /// <summary>
        /// Creates the visual GameObjects for teleportation (line and marker)
        /// and connects them to the locomotion system.
        /// </summary>
        private void CreateLocomotionVisuals(Transform rigParent, FluxVRLocomotion locomotion)
        {            
            // Create Line Renderer for Teleportation
            GameObject teleportLineGO = new GameObject("Teleport Line");
            teleportLineGO.transform.SetParent(rigParent, false);
            var lineRenderer = teleportLineGO.AddComponent<LineRenderer>();
            
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = new Color(0.8f, 1f, 1f, 0f); // Fade to transparent
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            
            locomotion.teleportLineRenderer = lineRenderer;
            
            // Create a simple Teleport Marker
            GameObject teleportMarkerGO = new GameObject("Teleport Marker");
            teleportMarkerGO.transform.SetParent(rigParent, true); // Parented with world position staying the same.
            
            GameObject markerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerVisual.transform.SetParent(teleportMarkerGO.transform, false);
            markerVisual.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            
            // --- Use the appropriate Destroy method based on the context ---
            var collider = markerVisual.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                // In Play Mode, use Destroy()
                Destroy(collider);
            }
            else
            {
                // In Edit Mode (e.g., when clicking the [FluxButton]), we must use DestroyImmediate()
                DestroyImmediate(collider);
            }
            
            locomotion.teleportMarkerPrefab = teleportMarkerGO;
            teleportMarkerGO.SetActive(false);
        }

        /// <summary>
        /// Safely destroys the created player instance.
        /// </summary>
        private void DestroyPlayerInstance()
        {
            if (_createdPlayer == null) return;
            
            if (Application.isPlaying)
            {
                Destroy(_createdPlayer);
            }
            else
            {
                DestroyImmediate(_createdPlayer);
            }
            _createdPlayer = null;
        }

        /// <summary>
        /// When this prefab builder is destroyed, ensure the player rig it created is also destroyed.
        /// </summary>
        protected override void OnFluxDestroy()
        {
            base.OnFluxDestroy();
            DestroyPlayerInstance();
        }
    }
}
#endif

  