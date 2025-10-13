    
#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.VR.Events;
using FluxFramework.Extensions;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// The XR namespace is kept for specific types like XRNode and HapticCapabilities.
using InputDevice = UnityEngine.XR.InputDevice;

namespace FluxFramework.VR
{
    /// <summary>
    /// Represents a single VR controller. This version has been refactored to use the new Input System exclusively
    /// and simplifies initialization logic to reduce boilerplate code.
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

        // A list to hold all created actions for easy cleanup.
        private readonly List<InputAction> _actionsToCleanup = new List<InputAction>();

        [Header("Haptic Feedback")]
        [Tooltip("Enables or disables haptic feedback for this controller.")]
        [SerializeField] private bool enableHaptics = true;
        [Tooltip("A global intensity multiplier for all haptic feedback on this controller.")]
        [SerializeField] [Range(0, 1)] private float hapticIntensity = 0.5f;

        private string _handIdentifier;
        private bool _isInitialized = false;
        
        public void Initialize(XRNode node, InputDevice device)
        {
            _controllerNode = node;
            _inputDevice = device;
            _handIdentifier = node == XRNode.LeftHand ? "left" : "right";
            
            // --- Programmatically create or get the reactive properties ---
            _velocityProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>($"vr.controller.{_handIdentifier}.velocity");
            _angularVelocityProp = Flux.Manager.Properties.GetOrCreateProperty<Vector3>($"vr.controller.{_handIdentifier}.angularVelocity");
            _isTrackedProp = Flux.Manager.Properties.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.isTracked");
            
            // --- Setup Input Actions using new helper methods for simplicity ---
            string handPath = (node == XRNode.LeftHand) ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";

            // Analog/Float Actions
            _triggerValueProp = SetupFloatAction(handPath, "trigger", 0.1f, new VRTriggerReleasedEvent(node), val => new VRTriggerPressedEvent(node, val));
            _gripValueProp = SetupFloatAction(handPath, "grip", 0.5f, new VRGripReleasedEvent(node), val => new VRGripPressedEvent(node, val));
            
            // Button Actions
            _primaryButtonPressedProp = SetupButtonAction(handPath, "primaryButton", VRButtonType.Primary);
            _secondaryButtonPressedProp = SetupButtonAction(handPath, "secondaryButton", VRButtonType.Secondary);
            _thumbstickPressedProp = SetupButtonAction(handPath, "thumbstickClicked", VRButtonType.ThumbstickClick);
            _menuButtonPressedProp = SetupButtonAction(handPath, "menu", VRButtonType.Menu);
            
            // Continuous Value Actions
            _thumbstickValueProp = SetupValueAction<Vector2>(handPath, "thumbstick");
            SetupValueAction<Vector3>(handPath, "deviceVelocity", prop => _velocityProp = prop);
            SetupValueAction<Vector3>(handPath, "deviceAngularVelocity", prop => _angularVelocityProp = prop);
            
            var isTrackedAction = new InputAction("isTracked", binding: $"{handPath}/isTracked");
            isTrackedAction.performed += ctx => _isTrackedProp.Value = ctx.ReadValueAsButton();
            isTrackedAction.canceled += ctx => _isTrackedProp.Value = false; // Ensure it's false when tracking is lost.
            isTrackedAction.Enable();
            _actionsToCleanup.Add(isTrackedAction);

            _isInitialized = true;
            
            Flux.Manager.Logger.Info($"FluxVRController initialized for {_handIdentifier} hand. Device: {device.name}", this);
        }

        public void Initialize(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            Initialize(node, device);
        }

        #region Action Setup Helpers

        // Helper for setting up a standard boolean button action.
        private IReactiveProperty<bool> SetupButtonAction(string handPath, string binding, VRButtonType buttonType)
        {
            var prop = Flux.Manager.Properties.GetOrCreateProperty<bool>($"vr.controller.{_handIdentifier}.{binding}");
            var action = new InputAction(binding, binding: $"{handPath}/{binding}");
            
            action.performed += ctx => UpdateButtonState(true, prop, new VRButtonPressedEvent(_controllerNode, buttonType));
            action.canceled += ctx => UpdateButtonState(false, prop, new VRButtonReleasedEvent(_controllerNode, buttonType));
            
            action.Enable();
            _actionsToCleanup.Add(action);
            return prop;
        }

        // Helper for setting up a float/axis action that behaves like a button with a threshold.
        private IReactiveProperty<float> SetupFloatAction(string handPath, string binding, float threshold, IFluxEvent releaseEvent, System.Func<float, IFluxEvent> pressEventFactory)
        {
            var prop = Flux.Manager.Properties.GetOrCreateProperty<float>($"vr.controller.{_handIdentifier}.{binding}");
            var action = new InputAction(binding, binding: $"{handPath}/{binding}");
            
            System.Action<InputAction.CallbackContext> updateAction = ctx => {
                float val = ctx.ReadValue<float>();
                UpdateInputValue(val, prop, threshold, pressEventFactory(val), releaseEvent);
            };

            action.performed += updateAction;
            action.canceled += updateAction;

            action.Enable();
            _actionsToCleanup.Add(action);
            return prop;
        }
        
        // Helper for setting up a continuous value action (like tracking data or thumbstick position).
        private IReactiveProperty<T> SetupValueAction<T>(string handPath, string binding, System.Action<IReactiveProperty<T>> setPropertyAction = null) where T : struct
        {
            var prop = Flux.Manager.Properties.GetOrCreateProperty<T>($"vr.controller.{_handIdentifier}.{binding}");
            setPropertyAction?.Invoke(prop); // Assigns the property back to the class member if needed.

            var action = new InputAction(binding, binding: $"{handPath}/{binding}");
            action.performed += ctx => prop.Value = ctx.ReadValue<T>();
            
            // For booleans from tracking, we also want to update on cancel (e.g., isTracked -> false)
            if (typeof(T) == typeof(bool))
            {
                action.canceled += ctx => prop.Value = default; // Sets to false
            }

            action.Enable();
            _actionsToCleanup.Add(action);
            return prop;
        }

        #endregion

        #region Input Update Helpers

        private void UpdateInputValue<T>(T newValue, IReactiveProperty<T> prop, float threshold, IFluxEvent pressEvent, IFluxEvent releaseEvent) where T : struct, System.IComparable
        {
            T oldValue = prop.Value;
            prop.Value = newValue;

            bool oldState = (oldValue.CompareTo(threshold) >= 0);
            bool newState = (newValue.CompareTo(threshold) >= 0);

            if (newState != oldState)
            {
                if (newState) this.PublishEvent(pressEvent);
                else this.PublishEvent(releaseEvent);
            }
        }

        private void UpdateButtonState(bool isPressed, IReactiveProperty<bool> prop, IFluxEvent evt)
        {
            if (prop.Value != isPressed)
            {
                prop.Value = isPressed;
                this.PublishEvent(evt);
            }
        }

        #endregion

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
        
        protected override void OnFluxDestroy()
        {
            // Loop through the list to disable all actions, ensuring no leaks.
            foreach (var action in _actionsToCleanup)
            {
                action.Disable();
            }
            _actionsToCleanup.Clear();
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

  