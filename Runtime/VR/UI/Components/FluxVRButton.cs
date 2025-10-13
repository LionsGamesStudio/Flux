#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.VR.Events;
using FluxFramework.Extensions;
using System;
using System.Collections;

namespace FluxFramework.VR.UI
{
    /// <summary>
    /// A VR-aware UI button that provides 3D press feedback, haptic response, and visual state changes.
    /// It works in conjunction with the VRUIInteractor system and subscribes to both EventBus and UnityEvents.
    /// </summary>
    [RequireComponent(typeof(Button), typeof(Image))]
    public class FluxVRButton : FluxUIComponent
    {
        [Header("VR Button Configuration")]
        [Tooltip("If true, the button will visually move inward when pressed.")]
        [SerializeField] private bool enable3DPress = true;
        [SerializeField] private float pressDepth = 0.01f;
        [SerializeField] private float animationSpeed = 15f;
        [SerializeField] private bool hapticFeedback = true;
        [SerializeField] private float hapticIntensity = 0.5f;
        
        [Header("Visual Feedback")]
        [Tooltip("If true, the button will scale up slightly when hovered over.")]
        [SerializeField] private bool scaleOnHover = true;
        [SerializeField] private float hoverScaleMultiplier = 1.1f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private Color pressedColor = new Color(0.6f, 0.8f, 1f);
        
        // --- Reactive Property References ---
        private IReactiveProperty<bool> _isPressedProp;
        private IReactiveProperty<bool> _isHoveredProp;
        
        // --- Component References & State ---
        private Button _button;
        private Image _buttonImage;
        private Vector3 _originalScale;
        
        // --- Event Subscription Handles ---
        // IDisposable handles are used for our custom EventBus system.
        private IDisposable _hoverEnterSub;
        private IDisposable _hoverExitSub;
        private IDisposable _clickSub;

        /// <summary>
        /// Gets component references, initializes reactive properties, and subscribes to standard UnityEvents.
        /// </summary>
        protected override void InitializeComponent()
        {
            _button = GetComponent<Button>();
            _buttonImage = GetComponent<Image>();
            _originalScale = transform.localScale;
            
            string buttonKey = $"{gameObject.name}_{GetInstanceID()}";
            _isPressedProp = Flux.Manager.Properties.GetOrCreateProperty<bool>($"vr.button.{buttonKey}.isPressed");
            _isHoveredProp = Flux.Manager.Properties.GetOrCreateProperty<bool>($"vr.button.{buttonKey}.isHovered");

            if(_buttonImage != null) _buttonImage.color = normalColor;

            // Subscribe to the standard Unity Button's onClick event.
            // This does not return IDisposable and must be cleaned up with RemoveListener.
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Subscribes to the Flux EventBus. This is done in OnFluxStart to ensure the EventBus is initialized.
        /// </summary>
        protected override void OnFluxStart()
        {
            _hoverEnterSub = Flux.Manager.EventBus.Subscribe<VRUIHoverEnterEvent>(OnHoverEnter);
            _hoverExitSub = Flux.Manager.EventBus.Subscribe<VRUIHoverExitEvent>(OnHoverExit);
            _clickSub = Flux.Manager.EventBus.Subscribe<VRUIClickEvent>(OnVRClick);
        }

        /// <summary>
        /// Updates the visual feedback of the button each frame.
        /// </summary>
        protected virtual void Update()
        {
            UpdateVisualFeedback();
        }

        /// <summary>
        /// Cleans up all event subscriptions when the object is destroyed.
        /// </summary>
        protected override void CleanupComponent()
        {
            // 1. Unsubscribe from the Flux EventBus using the stored IDisposable handles.
            _hoverEnterSub?.Dispose();
            _hoverExitSub?.Dispose();
            _clickSub?.Dispose();
            
            // 2. Unsubscribe from the Unity Button's onClick event using RemoveListener.
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        // --- Event Handlers ---

        private void OnHoverEnter(VRUIHoverEnterEvent evt)
        {
            if (evt.UIElement == gameObject)
            {
                _isHoveredProp.Value = true;
            }
        }

        private void OnHoverExit(VRUIHoverExitEvent evt)
        {
            if (evt.UIElement == gameObject)
            {
                _isHoveredProp.Value = false;
            }
        }

        private void OnVRClick(VRUIClickEvent evt)
        {
            if (evt.UIElement == gameObject && _button != null && _button.interactable)
            {
                // When a VR Pointer clicks us, we programmatically invoke the standard button's onClick event.
                // This makes the button work seamlessly with both VR pointers and traditional mouse clicks.
                _button.onClick.Invoke();
                
                _isPressedProp.Value = true;
                
                if (hapticFeedback)
                {
                    // FindObjectOfType is generally discouraged, but can be acceptable here for a self-contained component.
                    // A better approach would be to have a central VRManager reference passed in.
                    var vrManager = FindObjectOfType<FluxVRManager>();
                    var controller = vrManager?.GetController(evt.ControllerNode);
                    controller?.TriggerHapticFeedback(hapticIntensity, 0.1f);
                }
                
                StartCoroutine(ResetPressedState(0.15f));
            }
        }

        private IEnumerator ResetPressedState(float delay)
        {
            yield return new WaitForSeconds(delay);
            _isPressedProp.Value = false;
        }
        
        /// <summary>
        /// This method is called by the Unity Button's onClick event.
        /// </summary>
        private void OnButtonClicked()
        {
            // Publish a high-level event that game logic can listen to,
            // abstracting away whether the click came from VR or a mouse.
            this.PublishEvent(new VRButtonClickedEvent(gameObject));
            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxVRButton] {gameObject.name} onClick event fired.", this);
        }
        
        /// <summary>
        /// Smoothly interpolates the button's color and scale based on its current interaction state.
        /// </summary>
        private void UpdateVisualFeedback()
        {
            Color targetColor = normalColor;
            if (_isPressedProp.Value) targetColor = pressedColor;
            else if (_isHoveredProp.Value) targetColor = hoverColor;

            Vector3 targetScale = _originalScale;
            if (scaleOnHover && _isHoveredProp.Value && !_isPressedProp.Value)
            {
                targetScale *= hoverScaleMultiplier;
            }
            
            if (_buttonImage != null)
            {
                _buttonImage.color = Color.Lerp(_buttonImage.color, targetColor, Time.deltaTime * animationSpeed);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
    }

    // --- Associated Events ---

    /// <summary>
    /// Published when a FluxVRButton's onClick event is fired.
    /// </summary>
    public class VRButtonClickedEvent : FluxEventBase
    {
        public GameObject Button { get; }
        public VRButtonClickedEvent(GameObject button) : base("VRUIButton") { Button = button; }
    }
}
#endif