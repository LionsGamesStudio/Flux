#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;

namespace FluxFramework.VR
{
    /// <summary>
    /// Represents a single VR controller (Left or Right hand).
    /// This component polls the underlying XR InputDevice and exposes its tracking data and input states
    /// through the FluxFramework's reactive properties and event bus.
    /// It is typically spawned and managed by the FluxVRManager.
    /// </summary>
    public class FluxVRController : FluxMonoBehaviour
    {
        [Header("Controller State")]
        [Tooltip("The XRNode (LeftHand or RightHand) this controller represents.")]
        [SerializeField] private XRNode _controllerNode;
        public XRNode ControllerNode => _controllerNode;
        
        private InputDevice _inputDevice;
        public InputDevice InputDevice => _inputDevice;

        // --- Reactive Property References ---
        // We store direct references to the ReactiveProperties instead of the raw values.
        // This ensures that when we update them, the entire framework is notified.
        private IReactiveProperty<Vector3> _positionProp;
        private IReactiveProperty<Quaternion> _rotationProp;
        private IReactiveProperty<Vector3> _velocityProp;
        private IReactiveProperty<Vector3> _angularVelocityProp;
        private IReactiveProperty<bool> _isTrackedProp;
        private IReactiveProperty<float> _triggerValueProp;
        private IReactiveProperty<float> _gripValueProp;
        private IReactiveProperty<bool> _primaryButtonPressedProp;
        private IReactiveProperty<bool> _secondaryButtonPressedProp;
        private IReactiveProperty<Vector2> _thumbstickValueProp;
        private IReactiveProperty<bool> _thumbstickPressedProp;
        private IReactiveProperty<bool> _menuButtonPressedProp;

        [Header("Haptic Feedback")]
        [Tooltip("Enables or disables haptic feedback for this controller.")]
        [SerializeField] private bool enableHaptics = true;
        [Tooltip("A global intensity multiplier for all haptic feedback on this controller.")]
        [SerializeField] [Range(0, 1)] private float hapticIntensity = 0.5f;

        private string _handIdentifier;

        /// <summary>
        /// Initializes the controller for a specific hand and device.
        /// This is where the reactive properties are created with dynamic keys based on the controller's hand.
        /// </summary>
        public void Initialize(InputDevice device, XRNode node)
        {
            _inputDevice = device;
            _controllerNode = node;
            _handIdentifier = node == XRNode.LeftHand ? "left" : "right";
            
            // Programmatically create or get the reactive properties with dynamic keys.
            _positionProp = FluxManager.Instance.GetOrCreateProperty<Vector3>($"vr.controller.{_handIdentifier}.position");
            _rotationProp = FluxManager.Instance.GetOrCreateProperty<Quaternion>($"vr.controller.{_handIdentifier}.rotation");
            _velocityProp = FluxManager.Instance.GetOrCreateProperty<Vector3>($"vr.controller.{_handIdentifier}.velocity");
            _angularVelocityProp = FluxManager.Instance.GetOrCreateProperty<Vector3>($"vr.controller.{_handIdentifier}.angularVelocity");
            _isTrackedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.isTracked");
            _triggerValueProp = FluxManager.Instance.GetOrCreateProperty<float>($"vr.controller.{_handIdentifier}.trigger");
            _gripValueProp = FluxManager.Instance.GetOrCreateProperty<float>($"vr.controller.{_handIdentifier}.grip");
            _primaryButtonPressedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.primaryButton");
            _secondaryButtonPressedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.secondaryButton");
            _thumbstickValueProp = FluxManager.Instance.GetOrCreateProperty<Vector2>($"vr.controller.{_handIdentifier}.thumbstick");
            _thumbstickPressedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.thumbstickClick");
            _menuButtonPressedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.menuButton");

            Debug.Log($"[FluxFramework] FluxVRController initialized for {_handIdentifier} hand.", this);
        }
        
        /// <summary>
        /// We use LateUpdate to ensure tracking is updated after the main XR rig may have moved in Update.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (_inputDevice.isValid)
            {
                UpdateTracking();
                UpdateInputs();
            }
            else if (_isTrackedProp != null && _isTrackedProp.Value)
            {
                _isTrackedProp.Value = false;
            }
        }

        /// <summary>
        /// Polls the device for tracking data and updates the corresponding reactive properties.
        /// It also syncs the GameObject's transform to match the controller's physical location.
        /// </summary>
        public void UpdateTracking()
        {
            if (_inputDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                _positionProp.Value = pos;
                transform.localPosition = pos;
            }
            if (_inputDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                _rotationProp.Value = rot;
                transform.localRotation = rot;
            }
            if (_inputDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 vel))
                _velocityProp.Value = vel;
            if (_inputDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angVel))
                _angularVelocityProp.Value = angVel;
            if (_inputDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked))
                _isTrackedProp.Value = tracked;
        }

        /// <summary>
        /// Polls the device for input data and updates reactive properties, publishing events on state changes.
        /// </summary>
        private void UpdateInputs()
        {
            // Trigger
            if (_inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
            {
                float oldTrigger = _triggerValueProp.Value;
                if (!Mathf.Approximately(oldTrigger, trigger))
                {
                    _triggerValueProp.Value = trigger;
                    if (oldTrigger < 0.1f && trigger >= 0.1f) PublishEvent(new VRTriggerPressedEvent(_controllerNode, trigger));
                    else if (oldTrigger >= 0.1f && trigger < 0.1f) PublishEvent(new VRTriggerReleasedEvent(_controllerNode));
                }
            }
            
            // Grip
            if (_inputDevice.TryGetFeatureValue(CommonUsages.grip, out float grip))
            {
                float oldGrip = _gripValueProp.Value;
                 if (!Mathf.Approximately(oldGrip, grip))
                {
                    _gripValueProp.Value = grip;
                    if (oldGrip < 0.5f && grip >= 0.5f) PublishEvent(new VRGripPressedEvent(_controllerNode, grip));
                    else if (oldGrip >= 0.5f && grip < 0.5f) PublishEvent(new VRGripReleasedEvent(_controllerNode));
                }
            }
            
            // Buttons
            CheckButtonState(CommonUsages.primaryButton, _primaryButtonPressedProp, VRButtonType.Primary);
            CheckButtonState(CommonUsages.secondaryButton, _secondaryButtonPressedProp, VRButtonType.Secondary);
            CheckButtonState(CommonUsages.primary2DAxisClick, _thumbstickPressedProp, VRButtonType.ThumbstickClick);
            CheckButtonState(CommonUsages.menuButton, _menuButtonPressedProp, VRButtonType.Menu);
            
            // Thumbstick Axis
            if (_inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
            {
                _thumbstickValueProp.Value = thumbstick;
            }
        }
        
        /// <summary>
        /// Helper method to check a button's state and publish events.
        /// </summary>
        private void CheckButtonState(InputFeatureUsage<bool> usage, IReactiveProperty<bool> property, VRButtonType buttonType)
        {
            if (_inputDevice.TryGetFeatureValue(usage, out bool isPressed))
            {
                if (property.Value != isPressed)
                {
                    property.Value = isPressed;
                    if (isPressed) PublishEvent(new VRButtonPressedEvent(_controllerNode, buttonType));
                    else PublishEvent(new VRButtonReleasedEvent(_controllerNode, buttonType));
                }
            }
        }

        /// <summary>
        /// Triggers a haptic impulse on this controller.
        /// </summary>
        public void TriggerHapticFeedback(float amplitude = 0.5f, float duration = 0.1f)
        {
            if (enableHaptics && _inputDevice.isValid)
            {
                if (_inputDevice.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
                {
                    _inputDevice.SendHapticImpulse(0, amplitude * hapticIntensity, duration);
                }
            }
        }
        
        public Ray GetPointingRay() => new Ray(transform.position, transform.forward);
        public Vector3 GetVelocity() => _velocityProp.Value;
        public Vector3 GetAngularVelocity() => _angularVelocityProp.Value;
    }

    public enum VRButtonType
    {
        Primary,
        Secondary,
        Menu,
        ThumbstickClick,
        Trigger,
        Grip
    }
}
#endif