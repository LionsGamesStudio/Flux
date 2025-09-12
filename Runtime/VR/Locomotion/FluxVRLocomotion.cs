#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;
using System.Collections;

namespace FluxFramework.VR.Locomotion
{
    /// <summary>
    /// A comprehensive VR locomotion system that provides teleportation and smooth movement options.
    /// It is designed to be attached to the main VR Rig object alongside the FluxVRManager.
    /// </summary>
    [RequireComponent(typeof(FluxVRManager))]
    public class FluxVRLocomotion : FluxMonoBehaviour
    {
        [Header("General Locomotion")]
        [FluxGroup("General Settings")]
        [Tooltip("The primary method of movement for the player.")]
        [SerializeField] private LocomotionType locomotionType = LocomotionType.Teleport;
        
        [FluxGroup("Turning Settings")]
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
        [Tooltip("The thumbstick horizontal threshold to trigger a snap turn (e.g., 0.8 means 80% pushed).")]
        [SerializeField] [Range(0.1f, 1f)] private float snapTurnThreshold = 0.8f;

        [Header("Teleportation")]
        [FluxGroup("Teleportation Settings")]
        [Tooltip("The physics layers that the teleport ray can hit.")]
        [SerializeField] private LayerMask teleportLayerMask = -1;
        [FluxGroup("Teleportation Settings")]
        [Tooltip("The maximum distance the teleport arc can reach.")]
        [SerializeField] private float teleportMaxDistance = 10f;
        [FluxGroup("Teleportation Settings")]
        [Tooltip("A prefab to instantiate as the visual marker for the teleport destination.")]
        [SerializeField] private GameObject teleportMarkerPrefab;
        [FluxGroup("Teleportation Settings")]
        [Tooltip("The LineRenderer used to draw the teleportation arc.")]
        [SerializeField] private LineRenderer teleportLineRenderer;
        [FluxGroup("Teleportation Settings")]
        [Tooltip("The material to apply to the teleport LineRenderer.")]
        [SerializeField] private Material teleportLineMaterial;

        [Header("Smooth Movement")]
        [FluxGroup("Smooth Movement Settings")]
        [Tooltip("The movement speed in meters per second for smooth locomotion.")]
        [SerializeField] private float smoothMoveSpeed = 3f;
        [FluxGroup("Smooth Movement Settings")]
        [Tooltip("If true, smooth movement direction is based on where the controller is pointing. If false, it's based on where the HMD is looking.")]
        [SerializeField] private bool useControllerDirection = true;

        [Header("Comfort")]
        [FluxGroup("Comfort Settings")]
        [Tooltip("If true, the screen will briefly fade to black during a teleport to reduce disorientation.")]
        [SerializeField] private bool fadeOnTeleport = true;
        [FluxGroup("Comfort Settings")]
        [Tooltip("The total duration of the fade-out and fade-in effect for teleportation.")]
        [SerializeField] private float teleportFadeTime = 0.2f;
        
        // --- Reactive Property References ---
        private IReactiveProperty<Vector3> _playerPositionProp;
        private IReactiveProperty<float> _playerRotationProp;
        private IReactiveProperty<bool> _isMovingProp;
        private IReactiveProperty<float> _currentMovementSpeedProp;
        
        // --- Private State ---
        private FluxVRManager _vrManager;
        private Transform _vrRig;
        private Camera _vrCamera;
        private GameObject _teleportMarkerInstance;
        private bool _isAimingTeleport;
        private Vector3 _teleportDestination;
        private bool _isTeleportDestinationValid;
        private float _lastSnapTurnTime;
        private bool _isReadyForSnapTurn = true;
        
        protected override void Awake()
        {
            base.Awake();

            _vrManager = GetComponent<FluxVRManager>();
            _vrCamera = GetComponentInChildren<Camera>();
            if (_vrCamera != null)
            {
                // A common rig structure is [Rig]->[CameraOffset]->[Camera]
                // We assume the locomotion script is on the top-level Rig object.
                _vrRig = transform;
            }
            else
            {
                Debug.LogError("[FluxFramework] FluxVRLocomotion could not find a Camera in its children. Locomotion will be disabled.", this);
                enabled = false;
                return;
            }

            // Initialize reactive properties that will be updated by this system.
            _playerPositionProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>("vr.locomotion.position");
            _playerRotationProp = Flux.Manager.Properties.GetOrCreateProperty<float>("vr.locomotion.rotation");
            _isMovingProp = Flux.Manager.Properties.GetOrCreateProperty<bool>("vr.locomotion.isMoving");
            _currentMovementSpeedProp = Flux.Manager.Properties.GetOrCreateProperty<float>("vr.locomotion.movementSpeed");

            SetupTeleportationVisuals();
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
            
            if (teleportLineRenderer != null && teleportLineMaterial != null)
            {
                teleportLineRenderer.material = teleportLineMaterial;
                teleportLineRenderer.startColor = Color.cyan;
                teleportLineRenderer.endColor = Color.cyan;
                teleportLineRenderer.startWidth = 0.015f;
                teleportLineRenderer.endWidth = 0.005f;
                teleportLineRenderer.positionCount = 2; // Default to a straight line
                teleportLineRenderer.enabled = false;
            }
        }
        
        private void HandleLocomotionInput()
        {
            var leftController = _vrManager.GetController(XRNode.LeftHand);
            var rightController = _vrManager.GetController(XRNode.RightHand);

            // Smooth movement with left thumbstick
            if (locomotionType == LocomotionType.Smooth && leftController != null && leftController.InputDevice.isValid)
            {
                HandleSmoothMovement(leftController.InputDevice);
            }
            else
            {
                _isMovingProp.Value = false;
            }

            // Turning and Teleporting with right thumbstick
            if (rightController != null && rightController.InputDevice.isValid)
            {
                HandleTurning(rightController.InputDevice);
                if (locomotionType == LocomotionType.Teleport)
                {
                    HandleTeleportInput(rightController.InputDevice);
                }
            }
        }
        
        private void HandleSmoothMovement(InputDevice device)
        {
            if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick) && thumbstick.magnitude > 0.1f)
            {
                var controller = _vrManager.GetController(XRNode.LeftHand);
                Vector3 forward = useControllerDirection && controller != null ? controller.transform.forward : _vrCamera.transform.forward;
                
                forward.y = 0;
                forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                
                Vector3 movement = (forward * thumbstick.y + right * thumbstick.x);
                _vrRig.position += movement * smoothMoveSpeed * Time.deltaTime;

                _isMovingProp.Value = true;
                _currentMovementSpeedProp.Value = movement.magnitude * smoothMoveSpeed;
            }
            else
            {
                _isMovingProp.Value = false;
                _currentMovementSpeedProp.Value = 0f;
            }
        }

        private void HandleTurning(InputDevice device)
        {
            if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick)) return;

            float horizontalAxis = thumbstick.x;
            
            if (allowSmoothTurning && Mathf.Abs(horizontalAxis) > 0.1f)
            {
                float turnAmount = horizontalAxis * smoothTurnSpeed * Time.deltaTime;
                _vrRig.RotateAround(_vrCamera.transform.position, Vector3.up, turnAmount);
            }
            else if (allowSnapTurning)
            {
                if (_isReadyForSnapTurn && Mathf.Abs(horizontalAxis) > snapTurnThreshold)
                {
                    float turnAngle = Mathf.Sign(horizontalAxis) * snapTurnAngle;
                    _vrRig.RotateAround(_vrCamera.transform.position, Vector3.up, turnAngle);
                    _isReadyForSnapTurn = false;
                    _lastSnapTurnTime = Time.time;
                }
                else if (!_isReadyForSnapTurn && Mathf.Abs(horizontalAxis) < 0.1f)
                {
                     _isReadyForSnapTurn = true;
                }
                else if (!_isReadyForSnapTurn && Time.time - _lastSnapTurnTime > snapTurnCooldown)
                {
                    _isReadyForSnapTurn = true;
                }
            }
        }

        private void HandleTeleportInput(InputDevice device)
        {
            bool isAimingInput = device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick) && thumbstick.y > snapTurnThreshold;
            
            if (isAimingInput && !_isAimingTeleport)
            {
                StartTeleportAiming();
            }
            else if (!isAimingInput && _isAimingTeleport)
            {
                if (_isTeleportDestinationValid) ExecuteTeleport();
                else CancelTeleportAiming();
            }
        }

        private void StartTeleportAiming()
        {
            _isAimingTeleport = true;
            if (teleportLineRenderer != null) teleportLineRenderer.enabled = true;
            if (_teleportMarkerInstance != null) _teleportMarkerInstance.SetActive(true);
        }

        private void CancelTeleportAiming()
        {
            _isAimingTeleport = false;
            if (teleportLineRenderer != null) teleportLineRenderer.enabled = false;
            if (_teleportMarkerInstance != null) _teleportMarkerInstance.SetActive(false);
        }
        
        private void UpdateTeleportAim()
        {
            if (!_isAimingTeleport) return;

            var rightController = _vrManager.GetController(XRNode.RightHand);
            if (rightController == null) return;

            Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
            _isTeleportDestinationValid = Physics.Raycast(ray, out RaycastHit hit, teleportMaxDistance, teleportLayerMask);
            
            Vector3 endPoint = _isTeleportDestinationValid ? hit.point : ray.GetPoint(teleportMaxDistance);
            Color lineColor = _isTeleportDestinationValid ? Color.green : Color.red;

            if (_teleportMarkerInstance != null)
            {
                _teleportMarkerInstance.SetActive(_isTeleportDestinationValid);
                if (_isTeleportDestinationValid) _teleportMarkerInstance.transform.position = endPoint;
            }

            if (teleportLineRenderer != null) 
            {
                teleportLineRenderer.startColor = teleportLineRenderer.endColor = lineColor;
                teleportLineRenderer.SetPosition(0, rightController.transform.position);
                teleportLineRenderer.SetPosition(1, endPoint);
            }
        }
        
        private void ExecuteTeleport()
        {
            Vector3 oldPosition = _vrRig.position;
            
            if (fadeOnTeleport) StartCoroutine(TeleportWithFade());
            else PerformTeleport();
            
            PublishEvent(new VRTeleportEvent(oldPosition, _teleportDestination, XRNode.RightHand));
            _vrManager.GetController(XRNode.RightHand)?.TriggerHapticFeedback(0.8f, 0.2f);
            
            CancelTeleportAiming();
        }
        
        private void PerformTeleport()
        {
            Vector3 headOffset = _vrCamera.transform.position - _vrRig.position;
            headOffset.y = 0;
            _vrRig.position = _teleportDestination - headOffset;
        }
        
        private IEnumerator TeleportWithFade()
        {
            // Placeholder for a proper screen fade implementation (e.g., using a post-process effect or a simple black quad)
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
    }
    
    public enum LocomotionType
    {
        Teleport,
        Smooth
    }
}
#endif