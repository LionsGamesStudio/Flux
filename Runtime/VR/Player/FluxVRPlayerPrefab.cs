#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Locomotion;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem;

namespace FluxFramework.VR
{
    /// <summary>
    /// A factory/builder component that programmatically creates a complete,
    /// fully-functional VR Player Rig with all necessary FluxVR components,
    /// camera setup, and controller visuals.
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
        /// This is the main factory method.
        /// </summary>
        [FluxButton("Create Complete VR Player")]
        public void CreateVRPlayerPrefab()
        {
            if (_createdPlayer != null)
            {
                Flux.Manager.Logger.Warning("A VR Player instance created by this component already exists. It will be destroyed before creating a new one.", this);
                DestroyPlayerInstance();
            }

            if (vrPlayerPrefab != null)
            {
                _createdPlayer = Instantiate(vrPlayerPrefab, transform.position, transform.rotation);
                Flux.Manager.Logger.Info("Instantiated VR Player from the provided prefab.", this);
                return;
            }

            // --- 1. Create the root VR Rig GameObject ---
            GameObject vrRig = new GameObject("FluxVR Player (Generated)");
            vrRig.transform.position = Vector3.zero;

            // --- 2. Add and configure the CharacterController for physics and movement ---
            var characterController = vrRig.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.skinWidth = 0.01f;
            characterController.center = new Vector3(0, 0.9f, 0);

            // --- 3. Create the Camera Rig Hierarchy ([Rig] -> [Camera Offset] -> [Main Camera]) ---
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(vrRig.transform);
            cameraOffset.transform.localPosition = Vector3.zero;

            GameObject cameraGO = new GameObject("Main Camera");
            cameraGO.transform.SetParent(cameraOffset.transform);
            cameraGO.transform.localPosition = new Vector3(0, 1.6f, 0); // Average eye height
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<Camera>();
            cameraGO.AddComponent<AudioListener>();
            
            // Add and configure the TrackedPoseDriver for HMD tracking
            var hmdPoseDriver = cameraGO.AddComponent<TrackedPoseDriver>();
            var hmdPositionAction = new InputAction("HMD Position", binding: "<XRHMD>/centerEyePosition");
            var hmdRotationAction = new InputAction("HMD Rotation", binding: "<XRHMD>/centerEyeRotation");
            hmdPoseDriver.positionAction = hmdPositionAction;
            hmdPoseDriver.rotationAction = hmdRotationAction;
            hmdPositionAction.Enable();
            hmdRotationAction.Enable();

            // --- 4. Add the core FluxVR logic components to the rig ---
            var vrManager = vrRig.AddComponent<FluxVRManager>();
            var locomotion = vrRig.AddComponent<FluxVRLocomotion>();
            vrRig.AddComponent<FluxVRPlayer>();

            // --- 5. Link necessary references between components ---
            vrManager.CameraOffset = cameraOffset.transform;

            // --- 6. Create default teleportation visuals and assign them ---
            var teleportMarker = CreateDefaultTeleportMarker();
            teleportMarker.transform.SetParent(vrRig.transform); // Keep it organized
            locomotion.teleportMarkerPrefab = teleportMarker;

            // We will let the manager create the controllers and the locomotion system will find them
            // to attach its line renderer. We don't need to pre-assign it.
            
            _createdPlayer = vrRig;
        }

        /// <summary>
        /// Creates a default, visible teleport marker prefab.
        /// </summary>
        private GameObject CreateDefaultTeleportMarker()
        {
            var markerInstance = new GameObject("Teleport Marker (Prefab)");
            GameObject markerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerVisual.name = "Visual";
            markerVisual.transform.SetParent(markerInstance.transform, false);
            markerVisual.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            DestroyImmediate(markerVisual.GetComponent<Collider>()); // No collider needed on the visual

            var renderer = markerVisual.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.green;
            // Make it slightly transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0, 1, 0, 0.5f);
            renderer.material = mat;

            markerInstance.SetActive(false);
            return markerInstance;
        }

        /// <summary>
        /// Safely destroys the created player instance.
        /// </summary>
        private void DestroyPlayerInstance()
        {
            if (_createdPlayer == null) return;
            
            if (Application.isPlaying) Destroy(_createdPlayer);
            else DestroyImmediate(_createdPlayer);
            
            _createdPlayer = null;
        }

        protected override void OnFluxDestroy()
        {
            base.OnFluxDestroy();
            DestroyPlayerInstance();
        }
    }
}
#endif