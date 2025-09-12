#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Locomotion;
using FluxFramework.VR.Events;
using System;

namespace FluxFramework.VR
{
    /// <summary>
    /// The main logic component for the VR player. It coordinates between other VR systems
    /// (Manager, Locomotion, Interaction), exposes high-level player state via reactive properties,
    //  and handles basic interactions like pointing and clicking on objects.
    /// </summary>
    [RequireComponent(typeof(FluxVRManager), typeof(FluxVRLocomotion), typeof(CharacterController))]
    public class FluxVRPlayer : FluxMonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 10f;
        [SerializeField] private LayerMask interactionLayerMask = -1;
        
        // --- Reactive Property References ---
        private IReactiveProperty<Vector3> _hmdPositionProp;
        private IReactiveProperty<Quaternion> _hmdRotationProp;
        private IReactiveProperty<int> _activeControllersProp;
        private IReactiveProperty<bool> _isGroundedProp;
        
        // --- Component References ---
        private FluxVRManager _vrManager;
        private CharacterController _characterController;
        private Camera _hmdCamera;
        
        // --- Event Subscription Handles ---
        private IDisposable _controllerConnectedSub;
        private IDisposable _controllerDisconnectedSub;
        private IDisposable _triggerPressedSub;
        
        protected override void Awake()
        {
            base.Awake();

            // Get component dependencies
            _vrManager = GetComponent<FluxVRManager>();
            _characterController = GetComponent<CharacterController>();
            _hmdCamera = GetComponentInChildren<Camera>();

            // Initialize reactive properties
            _hmdPositionProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>("vr.player.hmd.position");
            _hmdRotationProp = Flux.Manager.Properties.GetOrCreateProperty<Quaternion>("vr.player.hmd.rotation");
            _activeControllersProp = Flux.Manager.Properties.GetOrCreateProperty<int>("vr.player.controllers.active");
            _isGroundedProp = Flux.Manager.Properties.GetOrCreateProperty<bool>("vr.player.isGrounded", true);
        }

        protected virtual void Start()
        {
            // Subscribe to VR events. Storing the IDisposable handle is crucial for cleanup.
            _controllerConnectedSub = EventBus.Subscribe<VRControllerConnectedEvent>(OnControllerConnected);
            _controllerDisconnectedSub = EventBus.Subscribe<VRControllerDisconnectedEvent>(OnControllerDisconnected);
            _triggerPressedSub = EventBus.Subscribe<VRTriggerPressedEvent>(OnTriggerPressed);
            
            Debug.Log("[FluxFramework] FluxVRPlayer initialized!", this);
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from all events to prevent memory leaks.
            _controllerConnectedSub?.Dispose();
            _controllerDisconnectedSub?.Dispose();
            _triggerPressedSub?.Dispose();
            base.OnDestroy();
        }

        protected virtual void Update()
        {
            UpdatePlayerTracking();
        }

        private void FixedUpdate()
        {
            // Use FixedUpdate for physics-based checks like grounding.
            UpdateGroundCheck();
        }

        private void UpdatePlayerTracking()
        {
            if (_hmdCamera != null)
            {
                _hmdPositionProp.Value = _hmdCamera.transform.position;
                _hmdRotationProp.Value = _hmdCamera.transform.rotation;
            }
            if (_vrManager != null)
            {
                _activeControllersProp.Value = _vrManager.GetController(XRNode.LeftHand) != null && _vrManager.GetController(XRNode.RightHand) != null ? 2 : 
                                               _vrManager.GetController(XRNode.LeftHand) != null || _vrManager.GetController(XRNode.RightHand) != null ? 1 : 0;
            }
        }
        
        private void UpdateGroundCheck()
        {
            if (_characterController != null)
            {
                _isGroundedProp.Value = _characterController.isGrounded;
            }
        }
        
        // --- Event Handlers ---
        private void OnControllerConnected(VRControllerConnectedEvent evt)
        {
            Debug.Log($"Player detected controller connection: {evt.ControllerNode}");
        }
        
        private void OnControllerDisconnected(VRControllerDisconnectedEvent evt)
        {
            Debug.Log($"Player detected controller disconnection: {evt.ControllerNode}");
        }
        
        private void OnTriggerPressed(VRTriggerPressedEvent evt)
        {
            // When a trigger is pressed, perform a raycast to interact with the world.
            PerformInteraction(evt.ControllerNode);
        }
        
        /// <summary>
        /// Performs a raycast from the specified controller to find and interact with IVRInteractable objects.
        /// </summary>
        private void PerformInteraction(XRNode controllerNode)
        {
            var controller = _vrManager?.GetController(controllerNode);
            if (controller != null)
            {
                Ray ray = controller.GetPointingRay();
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask))
                {
                    // Check if the hit object implements the interactable interface.
                    var interactable = hit.collider.GetComponent<IVRInteractable>();
                    if (interactable != null)
                    {
                        // Call the interaction method on the object.
                        interactable.OnVRInteract(controller);
                        controller.TriggerHapticFeedback(0.5f, 0.1f);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// A generic interface for any object that can be interacted with by the VR player's controller.
    /// </summary>
    public interface IVRInteractable
    {
        /// <summary>
        /// Called when the player points at this object and presses the trigger.
        /// </summary>
        /// <param name="controller">The controller that initiated the interaction.</param>
        void OnVRInteract(FluxVRController controller);
    }
}
#endif