using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using FluxFramework.Core;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;
using System.Collections;

namespace FluxFramework.VR.UI
{
    /// <summary>
    /// A specialized World-Space Canvas for VR, designed to be interacted with by controllers.
    /// It can optionally follow the HMD for user comfort.
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    public class FluxVRCanvas : FluxUIComponent
    {
        [Header("VR Canvas Configuration")]
        [Tooltip("If true, the canvas will smoothly follow the player's head-mounted display.")]
        [SerializeField] private bool followHMD = false;
        [Tooltip("The distance from the HMD at which the canvas will hover.")]
        [SerializeField] private float followDistance = 2.0f;
        [Tooltip("How quickly the canvas follows the HMD's rotation and position.")]
        [SerializeField] private float followSmoothness = 8.0f;
        
        // --- Reactive Property References ---
        private IReactiveProperty<bool> _isVisibleProp;
        private IReactiveProperty<float> _distanceFromHMDProp;
        private IReactiveProperty<bool> _isFocusedProp;

        // --- Component References ---
        private Canvas _canvas;
        private Transform _hmdTransform;
        private VRUIInteractor _leftInteractor;
        private VRUIInteractor _rightInteractor;
        
        protected override void InitializeComponent()
        {
            _canvas = GetComponent<Canvas>();
            _hmdTransform = Camera.main?.transform;

            // --- Initialize Reactive Properties ---
            string canvasKeyName = $"{gameObject.name}_{GetInstanceID()}";
            _isVisibleProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.ui.canvas.{canvasKeyName}.visible", _canvas.enabled);
            _distanceFromHMDProp = FluxManager.Instance.GetOrCreateProperty<float>($"vr.ui.canvas.{canvasKeyName}.distance");
            _isFocusedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.ui.canvas.{canvasKeyName}.focused");

            SetupVRCanvas();
        }

        protected virtual void Start()
        {
            // We set up interactors in Start to ensure the FluxVRManager and controllers are ready.
            var vrManager = FindObjectOfType<FluxVRManager>(); // This is one of the few acceptable uses of FindObjectOfType at initialization.
            if (vrManager != null)
            {
                SetupControllerInteractors(vrManager);
            }
        }

        protected virtual void LateUpdate()
        {
            if (followHMD && _hmdTransform != null)
            {
                UpdateFollowHMD();
            }
            UpdateStateProperties();
        }

        private void SetupVRCanvas()
        {
            if (_canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("[FluxFramework] FluxVRCanvas requires the Canvas Render Mode to be set to World Space.", this);
                _canvas.renderMode = RenderMode.WorldSpace;
            }
            _canvas.worldCamera = Camera.main;
        }

        private void SetupControllerInteractors(FluxVRManager vrManager)
        {
            var leftController = vrManager.GetController(XRNode.LeftHand);
            if (leftController != null)
            {
                _leftInteractor = leftController.gameObject.AddComponent<VRUIInteractor>();
                _leftInteractor.Initialize(this);
            }
            
            var rightController = vrManager.GetController(XRNode.RightHand);
            if (rightController != null)
            {
                _rightInteractor = rightController.gameObject.AddComponent<VRUIInteractor>();
                _rightInteractor.Initialize(this);
            }
        }

        private void UpdateFollowHMD()
        {
            Vector3 targetPosition = _hmdTransform.position + (_hmdTransform.forward * followDistance);
            Quaternion targetRotation = Quaternion.LookRotation(transform.position - _hmdTransform.position, _hmdTransform.up);
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothness);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSmoothness);
        }

        private void UpdateStateProperties()
        {
            if (_hmdTransform != null)
            {
                _distanceFromHMDProp.Value = Vector3.Distance(transform.position, _hmdTransform.position);
            }

            bool wasFocused = _isFocusedProp.Value;
            bool isCurrentlyFocused = (_leftInteractor?.IsInteractingOnCanvas(this) ?? false) || (_rightInteractor?.IsInteractingOnCanvas(this) ?? false);
            
            if (isCurrentlyFocused != wasFocused)
            {
                _isFocusedProp.Value = isCurrentlyFocused;
                PublishEvent(new VRCanvasFocusChangedEvent(gameObject, isCurrentlyFocused));
            }
        }
        

        public void Show(bool animated = true)
        {
            if (animated) StartCoroutine(AnimateVisibility(true));
            else
            {
                _canvas.enabled = true;
                _isVisibleProp.Value = true;
            }
        }

        public void Hide(bool animated = true)
        {
            if (animated) StartCoroutine(AnimateVisibility(false));
            else
            {
                _canvas.enabled = false;
                _isVisibleProp.Value = false;
            }
        }

        private IEnumerator AnimateVisibility(bool show)
        {
            var canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            float startAlpha = canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;
            
            if (show)
            {
                _canvas.enabled = true;
                _isVisibleProp.Value = true;
            }
            
            for (float t = 0; t < 1; t += Time.deltaTime / 0.3f)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
            canvasGroup.alpha = endAlpha;
            
            if (!show)
            {
                _canvas.enabled = false;
                _isVisibleProp.Value = false;
            }
        }
    }
}