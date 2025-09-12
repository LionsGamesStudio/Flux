#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;

namespace FluxFramework.VR.Examples
{
    /// <summary>
    /// An example of a physically pressable VR button that gives visual and haptic feedback.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class VRInteractiveButton : FluxMonoBehaviour, IVRInteractable
    {
        [Header("Button Settings")]
        [SerializeField] private float pressDepth = 0.02f;
        [SerializeField] private float pressSpeed = 10f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.green;

        // --- Reactive Property References ---
        private IReactiveProperty<bool> _isPressedProp;
        private IReactiveProperty<int> _pressCountProp;

        // --- Private State ---
        private Vector3 _originalPosition;
        private Renderer _buttonRenderer;

        protected override void Awake()
        {
            base.Awake();
            _originalPosition = transform.localPosition;
            _buttonRenderer = GetComponent<Renderer>();
            
            // --- Reactive Property Initialization ---
            string buttonKeyName = $"{gameObject.name}_{GetInstanceID()}";
            _isPressedProp = Flux.Manager.GetOrCreateProperty<bool>($"vr.button.{buttonKeyName}.isPressed");
            _pressCountProp = Flux.Manager.GetOrCreateProperty<int>($"vr.button.{buttonKeyName}.pressCount");
        }
        
        protected virtual void Update()
        {
            UpdateButtonVisuals();
        }

        /// <summary>
        /// Called by an interaction system when the button is "clicked".
        /// </summary>
        public void OnVRInteract(FluxVRController controller)
        {
            if (!_isPressedProp.Value)
            {
                PressButton(controller);
            }
        }
        
        private void PressButton(FluxVRController controller)
        {
            _isPressedProp.Value = true;
            _pressCountProp.Value++;
            
            controller?.TriggerHapticFeedback(0.7f, 0.1f);
            
            // Publish a specific event for this button press.
            PublishEvent(new VRInteractiveButtonEvent(gameObject, controller.ControllerNode));
            
            // Use a coroutine for the release to avoid issues if the object is disabled.
            StartCoroutine(ReleaseButtonAfterDelay(0.2f));
            
            Debug.Log($"VR Button {gameObject.name} pressed! Total presses: {_pressCountProp.Value}");
        }
        
        private System.Collections.IEnumerator ReleaseButtonAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _isPressedProp.Value = false;
        }
        
        private void UpdateButtonVisuals()
        {
            Vector3 targetPosition = _originalPosition;
            if (_isPressedProp.Value)
            {
                // Assuming the button moves along its local Z-axis (forward/back).
                targetPosition -= transform.forward * pressDepth;
            }
            
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * pressSpeed);
            
            if (_buttonRenderer != null)
            {
                Color targetColor = _isPressedProp.Value ? pressedColor : normalColor;
                _buttonRenderer.material.color = Color.Lerp(_buttonRenderer.material.color, targetColor, Time.deltaTime * pressSpeed);
            }
        }
    }

    /// <summary>
    /// Custom event published when a VRInteractiveButton is pressed.
    /// </summary>
    public class VRInteractiveButtonEvent : FluxEventBase
    {
        public GameObject ButtonObject { get; }
        public UnityEngine.XR.XRNode ControllerNode { get; }
        
        public VRInteractiveButtonEvent(GameObject buttonObject, UnityEngine.XR.XRNode controller) : base("VRInteraction")
        {
            ButtonObject = buttonObject;
            ControllerNode = controller;
        }
    }
}
#endif