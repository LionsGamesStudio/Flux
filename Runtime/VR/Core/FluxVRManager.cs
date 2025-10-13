#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using FluxFramework.Core;
using FluxFramework.VR.Events;
using FluxFramework.VR.Locomotion;
using FluxFramework.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// We still need the InputDevice type for some specific operations like device lookup and haptics.
using InputDevice = UnityEngine.XR.InputDevice;

namespace FluxFramework.VR
{
    /// <summary>
    /// Manages the entire VR system's lifecycle, including device detection and controller instantiation.
    /// This class acts as the central hub for the VR rig. It uses a prefab-based approach for spawning controllers
    /// and is responsible for injecting necessary dependencies into them, creating a robust and decoupled architecture.
    /// </summary>
    public class FluxVRManager : FluxMonoBehaviour
    {
        #region Inspector Fields
        
        [Header("VR Configuration")]
        [Tooltip("If true, the VR system will attempt to initialize automatically on Start.")]
        [SerializeField] private bool autoInitializeVR = true;
        
        [Tooltip("Sets the tracking space type for the XR rig. RoomScale is recommended for most applications.")]
        [SerializeField] private VRTrackingSpace trackingSpace = VRTrackingSpace.RoomScale;
        
        [Tooltip("The transform that serves as the root of the tracking space (usually the parent of the camera). Controllers will be parented here.")]
        [SerializeField] private Transform cameraOffset;
        
        [Header("Controller Prefabs")]
        [Tooltip("The complete prefab for the left hand controller. It must have a FluxVRController component attached.")]
        [SerializeField] private GameObject leftControllerPrefab;

        [Tooltip("The complete prefab for the right hand controller. It must have a FluxVRController component attached.")]
        [SerializeField] private GameObject rightControllerPrefab;

        [Header("Default Visuals (Fallback)")]
        [Tooltip("This material is used for the default left controller visual if no prefab is assigned. Important for projects using URP/HDRP where legacy shaders are not available.")]
        [SerializeField] private Material defaultLeftControllerMaterial;

        [Tooltip("This material is used for the default right controller visual if no prefab is assigned.")]
        [SerializeField] private Material defaultRightControllerMaterial;

        [Header("Simulator Settings")]
        [Tooltip("Enable continuous polling to detect controllers that are activated dynamically, which is essential for the XR Device Simulator.")]
        [SerializeField] private bool enableContinuousPolling = true;
        
        [Tooltip("The interval in seconds between device polls when using the simulator.")]
        [SerializeField] private float pollingInterval = 1f;
        
        #endregion

        public Transform CameraOffset { get => cameraOffset; set => cameraOffset = value; }

        #region Private Fields
        
        // A cached reference to the rig's locomotion system, used for dependency injection.
        private FluxVRLocomotion _locomotionSystem;

        // Reactive properties for broadcasting global VR state.
        private IReactiveProperty<Vector3> _hmdPositionProp;
        private IReactiveProperty<Quaternion> _hmdRotationProp;
        private IReactiveProperty<bool> _hmdIsTrackedProp;
        private IReactiveProperty<Bounds> _playAreaBoundsProp;
        private IReactiveProperty<int> _activeControllerCountProp;

        // Registry of currently active controller instances.
        private readonly Dictionary<XRNode, FluxVRController> _controllers = new Dictionary<XRNode, FluxVRController>();
        
        private Coroutine _pollingCoroutine;
        private int _lastDeviceCount = -1;
        private bool _xrInitialized = false;
        
        #endregion

        #region Unity Lifecycle

        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();
            
            // Cache a reference to the locomotion system. This is done once at startup
            // so it can be injected into newly spawned controllers later.
            _locomotionSystem = GetComponent<FluxVRLocomotion>();
            if (_locomotionSystem == null)
            {
                Flux.Manager.Logger.Warning("FluxVRManager could not find a FluxVRLocomotion component on this rig. Teleport interactions may fail.", this);
            }

            // Automatically find the camera offset if it hasn't been assigned manually.
            if (cameraOffset == null)
            {
                Camera vrCamera = GetComponentInChildren<Camera>();
                if (vrCamera != null && vrCamera.transform.parent != null)
                {
                    cameraOffset = vrCamera.transform.parent;
                }
            }
            
            // Initialize global reactive properties using a central constants class to avoid typos.
            _hmdPositionProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>(VRPropertyKeys.HMDPosition);
            _hmdRotationProp = Flux.Manager.Properties.GetOrCreateProperty<Quaternion>(VRPropertyKeys.HMDRotation);
            _hmdIsTrackedProp = Flux.Manager.Properties.GetOrCreateProperty<bool>(VRPropertyKeys.HMDIsTracked);
            _playAreaBoundsProp = Flux.Manager.Properties.GetOrCreateProperty<Bounds>(VRPropertyKeys.PlayAreaBounds);
            _activeControllerCountProp = Flux.Manager.Properties.GetOrCreateProperty<int>(VRPropertyKeys.ActiveControllerCount);
        }

        protected override void OnFluxStart()
        {
            if (autoInitializeVR)
            {
                StartCoroutine(InitializeVRCoroutine());
            }

            // Subscribe to native XR device events to trigger controller refreshes.
            InputDevices.deviceConnected += OnDeviceChanged;
            InputDevices.deviceDisconnected += OnDeviceChanged;
        }

        protected virtual void LateUpdate()
        {
            if (_xrInitialized)
            {
                UpdateHMDTracking();
                UpdateControllerCount();
            }
        }

        protected override void OnFluxDestroy()
        {
            if (_pollingCoroutine != null)
            {
                StopCoroutine(_pollingCoroutine);
            }
            // Always unsubscribe from events to prevent memory leaks.
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceDisconnected -= OnDeviceChanged;
            base.OnFluxDestroy();
        }
        
        #endregion

        #region VR Initialization

        /// <summary>
        /// Coroutine to handle the asynchronous initialization process of the XR Plugin Management system.
        /// </summary>
        private IEnumerator InitializeVRCoroutine()
        {
            var xrGeneralSettings = XRGeneralSettings.Instance;
            if (xrGeneralSettings == null || xrGeneralSettings.Manager == null)
            {
                Flux.Manager.Logger.Warning("XRGeneralSettings or its Manager is null. This is okay for the simulator but may indicate an issue.", this);
                _xrInitialized = true;
            }
            else
            {
                var xrManager = xrGeneralSettings.Manager;
                if (!xrManager.isInitializationComplete)
                {
                    yield return xrManager.InitializeLoader();
                }

                // It's crucial to start the active XR Loader to enable device communication.
                if (xrManager.activeLoader != null)
                {
                    xrManager.activeLoader.Start();
                }
                _xrInitialized = true;
            }
            
            yield return new WaitForSeconds(0.2f);

            // Configure all available input subsystems for the desired tracking space.
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (!subsystem.running) subsystem.Start();
                subsystem.TrySetTrackingOriginMode(trackingSpace == VRTrackingSpace.RoomScale ? TrackingOriginModeFlags.Floor : TrackingOriginModeFlags.Device);
            }

            yield return new WaitForSeconds(0.5f); // Allow time for devices to register after startup.
            
            DebugLogAllDevices();
            StartCoroutine(DelayedControllerRefresh());

            if (enableContinuousPolling)
            {
                _pollingCoroutine = StartCoroutine(ContinuousDevicePolling());
            }

            var hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            this.PublishEvent(new VRInitializedEvent(hmd.isValid, hmd.isValid ? hmd.name : "No HMD Detected"));
            Flux.Manager.Logger.Info($"FluxVRManager Initialized. HMD Active: {hmd.isValid}", this);
        }

        /// <summary>
        /// Waits a short duration before the first controller refresh to give the XR system time to initialize.
        /// Includes a retry mechanism for simulators where controllers may appear late.
        /// </summary>
        private IEnumerator DelayedControllerRefresh()
        {
            yield return new WaitForSeconds(0.5f);
            RefreshAllControllers();
            if (_controllers.Count == 0)
            {
                Flux.Manager.Logger.Warning("No controllers found on first attempt, retrying in 1s...", this);
                yield return new WaitForSeconds(1f);
                RefreshAllControllers();
            }
        }

        /// <summary>
        /// A repeating coroutine that checks for changes in the number of connected devices.
        /// This is the primary mechanism for detecting controllers in the XR Device Simulator.
        /// </summary>
        private IEnumerator ContinuousDevicePolling()
        {
            while (true)
            {
                yield return new WaitForSeconds(pollingInterval);
                var devices = new List<InputDevice>();
                InputDevices.GetDevices(devices);
                if (devices.Count != _lastDeviceCount)
                {
                    _lastDeviceCount = devices.Count;
                    RefreshAllControllers();
                }
            }
        }
        
        #endregion

        #region HMD Tracking

        /// <summary>
        /// Polls the HMD for tracking data and updates the corresponding reactive properties.
        /// </summary>
        private void UpdateHMDTracking()
        {
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.isValid)
            {
                // We explicitly use UnityEngine.XR.CommonUsages to resolve the namespace ambiguity with UnityEngine.InputSystem.
                headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out Vector3 pos);
                headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out Quaternion rot);
                headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked);
                
                _hmdPositionProp.Value = pos;
                _hmdRotationProp.Value = rot;
                _hmdIsTrackedProp.Value = tracked;
            }
            else if (_hmdIsTrackedProp.Value)
            {
                // If the device was valid and now isn't, ensure the tracked status is set to false.
                _hmdIsTrackedProp.Value = false;
            }
        }
        
        #endregion

        #region Controller Management

        /// <summary>
        /// Updates the reactive property for the number of active controllers.
        /// </summary>
        private void UpdateControllerCount()
        {
            if (_activeControllerCountProp.Value != _controllers.Count)
            {
                _activeControllerCountProp.Value = _controllers.Count;
            }
        }

        /// <summary>
        /// Callback for native XR device connection/disconnection events.
        /// </summary>
        private void OnDeviceChanged(InputDevice device)
        {
            RefreshAllControllers();
        }

        /// <summary>
        /// Scans for all connected controllers and reconciles them with the currently spawned instances.
        /// This method creates or destroys controller GameObjects as needed.
        /// </summary>
        private void RefreshAllControllers()
        {
            if (!_xrInitialized) return;

            var activeDevices = new List<InputDevice>();
            InputDevices.GetDevices(activeDevices);
            
            // Filter devices to find only those that are explicitly left or right controllers.
            var controllerFlags = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand;
            var leftControllers = activeDevices.Where(d => d.isValid && (d.characteristics & (controllerFlags | InputDeviceCharacteristics.Left)) == (controllerFlags | InputDeviceCharacteristics.Left)).ToList();
            var rightControllers = activeDevices.Where(d => d.isValid && (d.characteristics & (controllerFlags | InputDeviceCharacteristics.Right)) == (controllerFlags | InputDeviceCharacteristics.Right)).ToList();

            // Spawn controllers if they are detected and not already in our registry.
            if (leftControllers.Any() && !_controllers.ContainsKey(XRNode.LeftHand)) CreateController(leftControllers[0], XRNode.LeftHand);
            if (rightControllers.Any() && !_controllers.ContainsKey(XRNode.RightHand)) CreateController(rightControllers[0], XRNode.RightHand);

            // Determine which controllers should no longer exist.
            var validNodes = new HashSet<XRNode>();
            if (leftControllers.Any()) validNodes.Add(XRNode.LeftHand);
            if (rightControllers.Any()) validNodes.Add(XRNode.RightHand);
            
            // Find and remove any controllers in our registry that are no longer detected as valid.
            var nodesToRemove = _controllers.Keys.Except(validNodes).ToList();
            foreach (var node in nodesToRemove)
            {
                DestroyController(node);
            }
        }

        /// <summary>
        /// The main factory method for a single controller. It instantiates a prefab (or creates a default),
        /// sets it up in the scene, and then injects all necessary dependencies into its components.
        /// </summary>
        private void CreateController(InputDevice device, XRNode node)
        {
            if (_controllers.ContainsKey(node)) return;

            GameObject controllerGO;
            GameObject prefabToUse = (node == XRNode.LeftHand) ? leftControllerPrefab : rightControllerPrefab;
            
            // Step 1: Instantiate the controller from a prefab or create a default one.
            if (prefabToUse != null)
            {
                controllerGO = Instantiate(prefabToUse);
                controllerGO.name = $"FluxVR Controller ({node}) - From Prefab";
            }
            else
            {
                Flux.Manager.Logger.Info($"No prefab assigned for {node}. Creating a default controller procedurally.", this);
                controllerGO = CreateDefaultController(node);
            }
            
            // Step 2: Parent the controller to the camera offset to ensure it moves with the player rig correctly.
            controllerGO.transform.SetParent(cameraOffset, false);
            
            // Step 3: Get references to the core components on the newly created controller.
            var controller = controllerGO.GetComponent<FluxVRController>();
            if (controller == null)
            {
                Flux.Manager.Logger.Error($"Controller prefab for {node} is missing the required FluxVRController component! Destroying instance.", this);
                Destroy(controllerGO);
                return;
            }
            var interactor = controllerGO.GetComponent<VRSmartInteractor>();
            
            // Step 4: DEPENDENCY INJECTION. Pass the required references to the components.
            controller.Initialize(node, device);
            interactor?.Initialize(controller, _locomotionSystem);
            
            // Step 5: Register the fully initialized controller.
            _controllers[node] = controller;
            this.PublishEvent(new VRControllerConnectedEvent(node, device.name));
            Flux.Manager.Logger.Info($"✓ Successfully created and initialized controller for {node} ({device.name})", this);
        }

        /// <summary>
        /// A fallback factory method that builds a complete, default controller GameObject from scratch.
        /// This is used if no prefab is assigned in the inspector.
        /// </summary>
        private GameObject CreateDefaultController(XRNode node)
        {
            var controllerGO = new GameObject($"FluxVR Controller ({node}) - Default");
            
            controllerGO.AddComponent<FluxVRController>();
            controllerGO.AddComponent<VRSmartInteractor>();
            
            var poseDriver = controllerGO.AddComponent<TrackedPoseDriver>();
            
            string handPath = (node == XRNode.LeftHand) ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
            
            var positionAction = new InputAction("Position", binding: $"{handPath}/devicePosition");
            var rotationAction = new InputAction("Rotation", binding: $"{handPath}/deviceRotation");
            
            poseDriver.positionAction = positionAction;
            poseDriver.rotationAction = rotationAction;

            // The actions must be enabled to start receiving data.
            positionAction.Enable();
            rotationAction.Enable();

            var handVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handVisual.name = "Hand Visual";
            handVisual.transform.SetParent(controllerGO.transform, false);
            handVisual.transform.localScale = Vector3.one * 0.05f;
            Destroy(handVisual.GetComponent<Collider>());
            
            var renderer = handVisual.GetComponent<Renderer>();
            Material materialToUse = (node == XRNode.LeftHand) ? defaultLeftControllerMaterial : defaultRightControllerMaterial;

            if (materialToUse != null)
            {
                renderer.material = materialToUse;
            }
            else
            {
                renderer.material.color = node == XRNode.LeftHand ? Color.cyan : Color.magenta;
                Flux.Manager.Logger.Warning($"No default material assigned for {node}. Using a fallback color.", this);
            }

            var lineRendererGO = new GameObject("Interactor Ray");
            lineRendererGO.transform.SetParent(handVisual.transform, false);
            var lineRenderer = lineRendererGO.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = new Color(1, 1, 1, 0);
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.001f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;

            return controllerGO;
        }

        /// <summary>
        /// Destroys a controller GameObject and removes it from the registry.
        /// </summary>
        private void DestroyController(XRNode node)
        {
            if (_controllers.TryGetValue(node, out var controllerToDestroy) && controllerToDestroy != null)
            {
                Destroy(controllerToDestroy.gameObject);
                _controllers.Remove(node);
                this.PublishEvent(new VRControllerDisconnectedEvent(node));
                Flux.Manager.Logger.Info($"✗ Destroyed controller for {node}", this);
            }
        }
        
        #endregion

        #region Public API
        /// <summary>
        /// Gets the FluxVRController for a specific hand.
        /// </summary>
        public FluxVRController GetController(XRNode hand) => _controllers.TryGetValue(hand, out var c) ? c : null;

        /// <summary>
        /// Returns true if a valid HMD is connected and active.
        /// </summary>
        public bool IsVRActive => InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid;

        /// <summary>
        /// Manually forces a controller refresh. Useful for debugging.
        /// </summary>
        public void ForceRefreshControllers() => RefreshAllControllers();

        /// <summary>
        /// Logs all detected input devices with their characteristics (for debugging).
        /// </summary>
        public void DebugLogAllDevices()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            Flux.Manager.Logger.Info($"=== XR Device Scan (Total: {devices.Count}) ===", this);
            foreach (var device in devices)
            {
                Flux.Manager.Logger.Info($"  • {device.name} | Valid: {device.isValid} | Chars: {device.characteristics}", this);
            }
        }
        #endregion
    }

    /// <summary>
    /// Defines the tracking space configuration for the VR rig.
    /// </summary>
    public enum VRTrackingSpace
    {
        Stationary = 0,
        RoomScale = 1
    }

    /// <summary>
    /// Central repository for reactive property keys to avoid magic strings.
    /// </summary>
    public static partial class VRPropertyKeys
    {
        public const string HMDPosition = "vr.hmd.position";
        public const string HMDRotation = "vr.hmd.rotation";
        public const string HMDIsTracked = "vr.hmd.isTracked";
        public const string PlayAreaBounds = "vr.playarea.bounds";
        public const string ActiveControllerCount = "vr.controllers.count";
    }
}
#endif