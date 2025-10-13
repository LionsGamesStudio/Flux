    
#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.VR.Locomotion;
using FluxFramework.VR.Events;
using System;

namespace FluxFramework.VR
{
    /// <summary>
    /// The main logic component for the VR player rig. Its primary role is now to coordinate
    /// the CharacterController with the HMD's physical location and to expose high-level player state.
    /// All direct world interaction logic has been moved to controller-specific components like VRSmartInteractor.
    /// </summary>
    [RequireComponent(typeof(FluxVRManager), typeof(FluxVRLocomotion), typeof(CharacterController))]
    public class FluxVRPlayer : FluxMonoBehaviour
    {        
        // --- Reactive Property References ---
        private IReactiveProperty<Vector3> _hmdPositionProp;
        private IReactiveProperty<Quaternion> _hmdRotationProp;
        private IReactiveProperty<int> _activeControllersProp;
        private IReactiveProperty<bool> _isGroundedProp;
        
        // --- Component References ---
        private FluxVRManager _vrManager;
        private CharacterController _characterController;
        private Camera _hmdCamera;
        private InputDevice _hmdDevice;
        
        // --- Event Subscription Handles ---
        private IDisposable _controllerConnectedSub;
        private IDisposable _controllerDisconnectedSub;
        
        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();

            _vrManager = GetComponent<FluxVRManager>();
            _characterController = GetComponent<CharacterController>();
            _hmdCamera = GetComponentInChildren<Camera>();
            _hmdDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

            _hmdPositionProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>(VRPropertyKeys.PlayerHMDPosition);
            _hmdRotationProp = Flux.Manager.Properties.GetOrCreateProperty<Quaternion>(VRPropertyKeys.PlayerHMDRotation);
            _activeControllersProp = Flux.Manager.Properties.GetOrCreateProperty<int>(VRPropertyKeys.PlayerActiveControllers);
            _isGroundedProp = Flux.Manager.Properties.GetOrCreateProperty<bool>(VRPropertyKeys.PlayerIsGrounded, true);
        }

        protected override void OnFluxStart()
        {
            _controllerConnectedSub = Flux.Manager.EventBus.Subscribe<VRControllerConnectedEvent>(OnControllerConnected);
            _controllerDisconnectedSub = Flux.Manager.EventBus.Subscribe<VRControllerDisconnectedEvent>(OnControllerDisconnected);

            Flux.Manager.Logger.Info("[FluxFramework] FluxVRPlayer initialized!", this);
        }
        
        protected override void OnFluxDestroy()
        {
            _controllerConnectedSub?.Dispose();
            _controllerDisconnectedSub?.Dispose();
            base.OnFluxDestroy();
        }

        protected virtual void Update()
        {
            UpdatePlayerTracking();
            CenterCharacterControllerToHMD();
        }

        private void FixedUpdate()
        {
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
                int count = 0;
                if (_vrManager.GetController(XRNode.LeftHand) != null) count++;
                if (_vrManager.GetController(XRNode.RightHand) != null) count++;
                _activeControllersProp.Value = count;
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
            Flux.Manager.Logger.Info($"Player detected controller connection: {evt.ControllerNode}", this);
        }
        
        private void OnControllerDisconnected(VRControllerDisconnectedEvent evt)
        {
            Flux.Manager.Logger.Info($"Player detected controller disconnection: {evt.ControllerNode}", this);
        }

        /// <summary>
        /// This crucial method adjusts the CharacterController's position and height
        /// to match the HMD's position within the local rig space. This ensures the player's
        /// collision capsule always follows their head.
        /// </summary>
        private void CenterCharacterControllerToHMD()
        {
            if (!_hmdDevice.isValid)
            {
                _hmdDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (!_hmdDevice.isValid) return;
            }

            if (_hmdDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 centerEyePos))
            {
                // We update the CharacterController height to match the player's height.
                _characterController.height = centerEyePos.y;
                
                // We center the collision capsule (X, Z) directly under the HMD.
                // The center height (Y) is at half the height of the capsule.
                _characterController.center = new Vector3(centerEyePos.x, centerEyePos.y / 2.0f, centerEyePos.z);
            }
        }
    }
    
    /// <summary>
    /// A generic interface for any object that can be interacted with by the VR player's controller.
    /// This is still used by VRSmartInteractor.
    /// </summary>
    public interface IVRInteractable
    {
        /// <summary>
        /// Called when the player points at this object and presses the trigger.
        /// </summary>
        /// <param name="controller">The controller that initiated the interaction.</param>
        void OnVRInteract(FluxVRController controller);
    }

    /// <summary>
    /// Central repository for reactive property keys. We will add the player-specific keys here.
    /// </summary>
    public static partial class VRPropertyKeys // Using partial to conceptually merge with the other keys
    {
        public const string PlayerHMDPosition = "vr.player.hmd.position";
        public const string PlayerHMDRotation = "vr.player.hmd.rotation";
        public const string PlayerActiveControllers = "vr.player.controllers.active";
        public const string PlayerIsGrounded = "vr.player.isGrounded";
    }
}
#endif

  