#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;
using System.Collections.Generic;

namespace FluxFramework.VR.UI
{
    /// <summary>
    /// Attached to a FluxVRController to enable it to interact with world-space VR canvases.
    /// It draws a laser pointer and simulates standard Unity UI events.
    /// </summary>
    public class VRUIInteractor : FluxMonoBehaviour
    {
        [Header("Interaction Configuration")]
        [SerializeField] private LineRenderer laserRenderer;
        [SerializeField] private float maxRayDistance = 10f;

        // --- Reactive Property References ---
        private IReactiveProperty<bool> _isInteractingProp;
        private IReactiveProperty<float> _hitDistanceProp;
        private IReactiveProperty<string> _currentElementProp;
        
        // --- Private State ---
        private FluxVRController _controller;
        private PointerEventData _pointerEventData;
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private GameObject _currentHitObject;
        private GameObject _lastPressedObject;
        private bool _isTriggerPressed;
        
        /// <summary>
        /// Initializes the interactor. Called by FluxVRCanvas.
        /// </summary>
        public void Initialize(FluxVRCanvas canvas)
        {
            _controller = GetComponent<FluxVRController>();
            _pointerEventData = new PointerEventData(EventSystem.current);
            
            // Setup a default laser if one is not provided.
            if (laserRenderer == null) CreateDefaultLaser();
            
            // Init reactive properties with dynamic key
            string hand = _controller.ControllerNode == XRNode.LeftHand ? "left" : "right";
            _isInteractingProp = Flux.Manager.GetOrCreateProperty<bool>($"vr.interactor.{hand}.isInteracting");
            _hitDistanceProp = Flux.Manager.GetOrCreateProperty<float>($"vr.interactor.{hand}.hitDistance");
            _currentElementProp = Flux.Manager.GetOrCreateProperty<string>($"vr.interactor.{hand}.currentElement");
        }

        protected virtual void Update()
        {
            if (_controller == null || !_controller.InputDevice.isValid)
            {
                if (_isInteractingProp.Value) DisableInteraction();
                return;
            }

            ProcessInteraction();
        }

        private void ProcessInteraction()
        {
            // Setup pointer data based on controller's current state
            _pointerEventData.position = new Vector2(Screen.width / 2f, Screen.height / 2f); // Center of screen
            _pointerEventData.pointerCurrentRaycast = PerformRaycast();
            
            // Update hover state
            ProcessHover();
            
            // Update press state
            ProcessPress();

            // Update laser visuals
            UpdateLaser();
        }

        private RaycastResult PerformRaycast()
        {
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);
            return FindFirstValidRaycast(_raycastResults);
        }

        private RaycastResult FindFirstValidRaycast(List<RaycastResult> results)
        {
            foreach (var result in results)
            {
                if (result.gameObject != null) return result;
            }
            return new RaycastResult(); // Return an empty result
        }

        private void ProcessHover()
        {
            GameObject newHitObject = _pointerEventData.pointerCurrentRaycast.gameObject;

            if (_currentHitObject != newHitObject)
            {
                // Exit the old object if it exists
                if (_currentHitObject != null)
                {
                    ExecuteEvents.Execute(_currentHitObject, _pointerEventData, ExecuteEvents.pointerExitHandler);
                    PublishEvent(new VRUIHoverExitEvent(_controller.ControllerNode, _currentHitObject));
                }

                _currentHitObject = newHitObject;

                // Enter the new object if it exists
                if (_currentHitObject != null)
                {
                    ExecuteEvents.Execute(_currentHitObject, _pointerEventData, ExecuteEvents.pointerEnterHandler);
                    PublishEvent(new VRUIHoverEnterEvent(_controller.ControllerNode, _currentHitObject));
                }
            }
            
            // Update reactive properties
            _isInteractingProp.Value = _currentHitObject != null;
            _currentElementProp.Value = _currentHitObject != null ? _currentHitObject.name : "";
            _hitDistanceProp.Value = _currentHitObject != null ? _pointerEventData.pointerCurrentRaycast.distance : 0;
        }
        
        private void ProcessPress()
        {
            bool triggerState = _controller.InputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed;

            if (triggerState && !_isTriggerPressed) // Press Down
            {
                _isTriggerPressed = true;
                _pointerEventData.pressPosition = _pointerEventData.position;
                _pointerEventData.pointerPressRaycast = _pointerEventData.pointerCurrentRaycast;
                
                _lastPressedObject = ExecuteEvents.GetEventHandler<IPointerDownHandler>(_currentHitObject);
                if (_lastPressedObject != null)
                {
                    ExecuteEvents.Execute(_lastPressedObject, _pointerEventData, ExecuteEvents.pointerDownHandler);
                    PublishEvent(new VRUIClickEvent(_controller.ControllerNode, _lastPressedObject, _pointerEventData.pointerCurrentRaycast.worldPosition));
                }
            }
            else if (!triggerState && _isTriggerPressed) // Press Up
            {
                _isTriggerPressed = false;
                
                var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(_currentHitObject);
                if (_lastPressedObject == clickHandler && _lastPressedObject != null)
                {
                    ExecuteEvents.Execute(_lastPressedObject, _pointerEventData, ExecuteEvents.pointerClickHandler);
                }

                if (_lastPressedObject != null)
                {
                    ExecuteEvents.Execute(_lastPressedObject, _pointerEventData, ExecuteEvents.pointerUpHandler);
                }

                _lastPressedObject = null;
            }
        }

        private void UpdateLaser()
        {
            if (laserRenderer == null) return;
            
            Vector3 startPoint = _controller.transform.position;
            Vector3 endPoint = _isInteractingProp.Value ? 
                _pointerEventData.pointerCurrentRaycast.worldPosition : 
                startPoint + _controller.transform.forward * maxRayDistance;

            laserRenderer.SetPosition(0, startPoint);
            laserRenderer.SetPosition(1, endPoint);
        }

        private void DisableInteraction()
        {
            ProcessHover(); // This will trigger an exit on the current object
            _isInteractingProp.Value = false;
            _currentElementProp.Value = "";
            _hitDistanceProp.Value = 0;
        }

        private void CreateDefaultLaser()
        {
            var laserGO = new GameObject("VR Laser");
            laserGO.transform.SetParent(transform, false);
            laserRenderer = laserGO.AddComponent<LineRenderer>();
            laserRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            laserRenderer.startColor = Color.cyan;
            laserRenderer.endColor = Color.white;
            laserRenderer.startWidth = 0.005f;
            laserRenderer.endWidth = 0.001f;
            laserRenderer.positionCount = 2;
            laserRenderer.useWorldSpace = true;
        }
        
        public bool IsInteractingOnCanvas(FluxVRCanvas canvas) => _isInteractingProp.Value && _pointerEventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<Canvas>() == canvas.GetComponent<Canvas>();
    }
}
#endif