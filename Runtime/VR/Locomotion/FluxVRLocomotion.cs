#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.VR.Events;
using FluxFramework.Extensions;
using UnityEngine.InputSystem;
using System;
using System.Collections;

using InputDevice = UnityEngine.XR.InputDevice;

namespace FluxFramework.VR.Locomotion
{
    /// <summary>
    /// A comprehensive VR locomotion system providing teleportation and smooth movement.
    /// Use CharacterController.Move() for teleportation, preventing physics issues.
    /// </summary>
    [RequireComponent(typeof(FluxVRManager))]
    public class FluxVRLocomotion : FluxMonoBehaviour
    {
        #region Inspector Fields
        [Header("General Locomotion")]
        [Tooltip("The primary method of movement for the player.")]
        [SerializeField] private LocomotionType locomotionType = LocomotionType.Teleport;
        
        [Header("Turning Settings")]
        [Tooltip("If true, allows for continuous, smooth turning using the right thumbstick.")]
        [SerializeField] private bool allowSmoothTurning = false;
        [Tooltip("If true, allows for incremental, instant turning using the right thumbstick.")]
        [SerializeField] private bool allowSnapTurning = true;
        [Tooltip("The speed for smooth turning, in degrees per second.")]
        [SerializeField] private float smoothTurnSpeed = 60f;
        [Tooltip("The fixed angle for each snap turn, in degrees.")]
        [SerializeField] private float snapTurnAngle = 30f;
        [Tooltip("The cooldown period in seconds between snap turns to prevent rapid spinning.")]
        [SerializeField] private float snapTurnCooldown = 0.3f;
        [Tooltip("The thumbstick horizontal threshold to trigger a snap turn.")]
        [SerializeField] [Range(0.1f, 1f)] private float snapTurnThreshold = 0.8f;

        [Header("Teleportation")]
        [Tooltip("The physics layers that the teleport ray can hit.")]
        [SerializeField] private LayerMask teleportLayerMask = -1;
        [Tooltip("The maximum distance the teleport arc can reach.")]
        [SerializeField] private float teleportMaxDistance = 10f;
        [Tooltip("If true, uses a straight ray for teleportation. If false, uses a parabolic arc.")]
        [SerializeField] private bool useStraightRay = true;
        [Tooltip("A prefab to instantiate as the visual marker for the teleport destination.")]
        [SerializeField] public GameObject teleportMarkerPrefab;
        
        [Header("Smooth Movement")]
        [Tooltip("The movement speed in meters per second for smooth locomotion.")]
        [SerializeField] private float smoothMoveSpeed = 3f;
        [Tooltip("If true, smooth movement is based on where the controller is pointing. If false, it's based on where the HMD is looking.")]
        [SerializeField] private bool useControllerDirection = true;

        [Header("Comfort")]
        [Tooltip("If true, the screen will briefly fade to black during a teleport to reduce disorientation.")]
        [SerializeField] private bool fadeOnTeleport = true;
        [Tooltip("The total duration of the fade-out and fade-in effect for teleportation.")]
        [SerializeField] private float teleportFadeTime = 0.2f;
        #endregion

        #region Private State
        private IReactiveProperty<Vector3> _playerPositionProp;
        private IReactiveProperty<float> _playerRotationProp;
        private IReactiveProperty<bool> _isMovingProp;
        private IReactiveProperty<float> _currentMovementSpeedProp;

        private InputAction _moveAction;
        private InputAction _teleportAction;
        private InputAction _turnAction;
        
        private FluxVRManager _vrManager;
        private Transform _vrRig;
        private Camera _vrCamera;
        private CharacterController _characterController;
        private GameObject _teleportMarkerInstance;
        private bool _isAimingTeleport;
        private Vector3 _teleportDestination;
        private bool _isTeleportDestinationValid;
        private float _lastSnapTurnTime;
        private bool _isReadyForSnapTurn = true;
        private VRTeleportSurface _currentTargetedSurface;
        #endregion
        
        protected override void OnFluxAwake()
        {
            base.OnFluxAwake();

            _characterController = GetComponent<CharacterController>();
            _vrManager = GetComponent<FluxVRManager>();
            _vrCamera = GetComponentInChildren<Camera>();
            if (_vrCamera != null)
            {
                _vrRig = transform;
            }
            else
            {
                Flux.Manager.Logger.Error("[FluxVRLocomotion] could not find a Camera in its children. Locomotion will be disabled.", this);
                enabled = false;
                return;
            }

            _playerPositionProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>("vr.locomotion.position");
            _playerRotationProp = Flux.Manager.Properties.GetOrCreateProperty<float>("vr.locomotion.rotation");
            _isMovingProp = Flux.Manager.Properties.GetOrCreateProperty<bool>("vr.locomotion.isMoving");
            _currentMovementSpeedProp = Flux.Manager.Properties.GetOrCreateProperty<float>("vr.locomotion.movementSpeed");

            _moveAction = new InputAction("Move", binding: "<XRController>{LeftHand}/primary2DAxis");
            _turnAction = new InputAction("Turn", binding: "<XRController>{RightHand}/primary2DAxis");
            _teleportAction = new InputAction("Teleport Aim", type: InputActionType.Button);
            _teleportAction.AddBinding("<XRController>{RightHand}/primary2DAxis/y").WithProcessors("axisDeadzone(min=0.5)");
            _teleportAction.performed += ctx => StartTeleportAiming();
            _teleportAction.canceled += ctx => FinalizeTeleport();
            _moveAction.Enable();
            _turnAction.Enable();
            _teleportAction.Enable();

            SetupTeleportationVisuals();
        }

        protected override void OnFluxDestroy()
        {
            base.OnFluxDestroy();
            _moveAction?.Disable();
            _turnAction?.Disable();
            _teleportAction?.Disable();
        }
        
        protected virtual void Update()
        {
            if (_vrRig == null) return;
            
            HandleLocomotionInput();
            UpdateTeleportAim();
            UpdateReactiveProperties();
        }
        
        private void SetupTeleportationVisuals()
        {
            if (teleportMarkerPrefab != null)
            {
                _teleportMarkerInstance = Instantiate(teleportMarkerPrefab);
                _teleportMarkerInstance.SetActive(false);
            }
        }
        
        private void HandleLocomotionInput()
        {
            var leftController = _vrManager.GetController(XRNode.LeftHand);
            var rightController = _vrManager.GetController(XRNode.RightHand);

            if (locomotionType == LocomotionType.Smooth && leftController != null)
            {
                HandleSmoothMovement();
            }
            else
            {
                _isMovingProp.Value = false;
            }

            if (rightController != null)
            {
                HandleTurning();
            }
        }
        
        private void HandleSmoothMovement()
        {
            Vector2 thumbstick = _moveAction.ReadValue<Vector2>();
            if (thumbstick.magnitude > 0.1f)
            {
                var controller = _vrManager.GetController(XRNode.LeftHand);
                Vector3 forward = useControllerDirection && controller != null ? controller.transform.forward : _vrCamera.transform.forward;
                
                // We only want to move along the horizontal plane, so we zero out the Y component.
                forward.y = 0;
                forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                
                Vector3 movement = (forward * thumbstick.y + right * thumbstick.x);
                if (_characterController != null)
                {
                    _characterController.Move(movement * smoothMoveSpeed * Time.deltaTime);
                }

                _isMovingProp.Value = true;
                _currentMovementSpeedProp.Value = movement.magnitude * smoothMoveSpeed;
            }
            else
            {
                _isMovingProp.Value = false;
                _currentMovementSpeedProp.Value = 0f;
            }
        }

        private void HandleTurning()
        {
            Vector2 thumbstick = _turnAction.ReadValue<Vector2>();
            float horizontalAxis = thumbstick.x;
            
            if (allowSmoothTurning && Mathf.Abs(horizontalAxis) > 0.1f)
            {
                float turnAmount = horizontalAxis * smoothTurnSpeed * Time.deltaTime;
                _vrRig.RotateAround(_vrCamera.transform.position, Vector3.up, turnAmount);
            }
            else if (allowSnapTurning)
            {
                // Manage the snap turn state to prevent turning on every frame.
                if (_isReadyForSnapTurn && Mathf.Abs(horizontalAxis) > snapTurnThreshold)
                {
                    float turnAngle = Mathf.Sign(horizontalAxis) * snapTurnAngle;
                    _vrRig.RotateAround(_vrCamera.transform.position, Vector3.up, turnAngle);
                    _isReadyForSnapTurn = false; // Enter cooldown
                    _lastSnapTurnTime = Time.time;
                }
                else if (!_isReadyForSnapTurn && Mathf.Abs(horizontalAxis) < 0.1f)
                {
                    _isReadyForSnapTurn = true; // Reset when stick returns to center
                }
                else if (!_isReadyForSnapTurn && Time.time - _lastSnapTurnTime > snapTurnCooldown)
                {
                    _isReadyForSnapTurn = true; // Reset after cooldown
                }
            }
        }

        private void StartTeleportAiming()
        {
            if (locomotionType != LocomotionType.Teleport) return;
            _isAimingTeleport = true;
            // The interactor's ray is already active. This system will now just check for teleport surfaces.
        }

        private void FinalizeTeleport()
        {
            if (!_isAimingTeleport) return;
            
            if (_isTeleportDestinationValid)
            {
                ExecuteTeleport();
            }
            
            if (_currentTargetedSurface != null)
            {
                _currentTargetedSurface.OnTargetExit();
                _currentTargetedSurface = null;
            }
            
            _isAimingTeleport = false;
            if (_teleportMarkerInstance != null)
            {
                _teleportMarkerInstance.SetActive(false);
            }
        }
        
        private void UpdateTeleportAim()
        {
            if (!_isAimingTeleport) return;

            var rightController = _vrManager.GetController(XRNode.RightHand);
            if (rightController == null) return;
            
            // This logic is somewhat redundant with the interactor, but keeps teleport-specific checks here.
            // An advanced optimization could be to get the hit result directly from the interactor.
            Vector3 startPos = rightController.transform.position;
            Vector3 direction = rightController.transform.forward;
            _isTeleportDestinationValid = false;
            
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, teleportMaxDistance, teleportLayerMask))
            {
                _teleportDestination = hit.point;
                var teleportSurface = hit.collider.GetComponent<VRTeleportSurface>();
                if (teleportSurface != null && teleportSurface.IsEnabled)
                {
                    _isTeleportDestinationValid = true;
                }
            }
            
            if (_teleportMarkerInstance != null)
            {
                _teleportMarkerInstance.SetActive(_isTeleportDestinationValid);
                if (_isTeleportDestinationValid)
                {
                    _teleportMarkerInstance.transform.position = _teleportDestination;
                }
            }
        }
        
        private void ExecuteTeleport()
        {
            Vector3 oldPosition = _vrRig.position;
            
            if (fadeOnTeleport)
            {
                StartCoroutine(TeleportWithFade());
            }
            else
            {
                PerformTeleport();
            }
            
            this.PublishEvent(new VRTeleportEvent(oldPosition, _teleportDestination, XRNode.RightHand));
            _vrManager.GetController(XRNode.RightHand)?.TriggerHapticFeedback(0.8f, 0.2f);
        }

        /// <summary>
        /// Moves the player to the teleport destination using the CharacterController. This is the safe way
        /// to move a physics-based character to prevent it from falling through the floor.
        /// </summary>
        private void PerformTeleport()
        {
            if (_characterController == null) return;

            // Calculate the horizontal offset between the player's head (camera) and their feet (rig origin).
            Vector3 headOffset = _vrCamera.transform.position - _vrRig.position;
            headOffset.y = 0;

            // The target position for the rig's origin is the teleport destination minus the head offset.
            // This ensures the player's head ends up at the destination point.
            Vector3 targetRigPosition = _teleportDestination - headOffset;

            // Calculate the vector needed to move from the current position to the target.
            Vector3 movementVector = targetRigPosition - _vrRig.position;
            
            // Use the CharacterController's Move method to safely apply the translation.
            _characterController.Move(movementVector);
        }
        
        private IEnumerator TeleportWithFade()
        {
            // This is a placeholder for a proper screen fade effect (e.g., using a black UI panel or post-processing).
            yield return new WaitForSeconds(teleportFadeTime / 2);
            PerformTeleport();
            yield return new WaitForSeconds(teleportFadeTime / 2);
        }
        
        private void UpdateReactiveProperties()
        {
            if (_vrRig != null)
            {
                _playerPositionProp.Value = _vrRig.position;
                _playerRotationProp.Value = _vrRig.eulerAngles.y;
            }
        }
        
        /// <summary>
        /// Public method that allows other systems (like an interactor) to trigger a teleport.
        /// </summary>
        public void TeleportToPosition(Vector3 targetPosition)
        {
            Vector3 oldPosition = _vrRig.position;
            _teleportDestination = targetPosition;
            PerformTeleport();
            this.PublishEvent(new VRTeleportEvent(oldPosition, _teleportDestination, XRNode.RightHand));
            _vrManager.GetController(XRNode.RightHand)?.TriggerHapticFeedback(0.8f, 0.2f);
        }
    }
    
    public enum LocomotionType
    {
        Teleport,
        Smooth
    }
}
#endif

  