using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;
using System;

namespace FluxFramework.VR.Examples
{
    /// <summary>
    /// An example of a VR interactable object. It can be hovered, grabbed, and thrown.
    /// Its state (isGrabbed, isHovered) is exposed via reactive properties.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class VRInteractableObject : FluxMonoBehaviour, IVRInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool canBeThrown = true;
        [SerializeField] private bool showOutlineOnHover = true;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer objectRenderer;
        [SerializeField] private Material outlineMaterial;
        
        // --- Reactive Property References ---
        private IReactiveProperty<bool> _isGrabbedProp;
        private IReactiveProperty<bool> _isHoveredProp;
        private IReactiveProperty<string> _grabbingControllerProp;

        // --- Private State ---
        private Rigidbody _rigidbody;
        private Material _originalMaterial;
        private Transform _originalParent;
        private bool _wasKinematic;
        private FluxVRController _currentGrabbingController;
        private IDisposable _hoverEnterSub;
        private IDisposable _hoverExitSub;

        protected override void Awake()
        {
            base.Awake();

            // Auto-find components
            _rigidbody = GetComponent<Rigidbody>();
            if (objectRenderer == null) objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null) _originalMaterial = objectRenderer.material;
                
            _originalParent = transform.parent;
            
            // --- Reactive Property Initialization ---
            // Using a unique key for each object is important. Here we use the instance ID.
            string objectKeyName = $"{gameObject.name}_{GetInstanceID()}";
            _isGrabbedProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.object.{objectKeyName}.isGrabbed");
            _isHoveredProp = FluxManager.Instance.GetOrCreateProperty<bool>($"vr.object.{objectKeyName}.isHovered");
            _grabbingControllerProp = FluxManager.Instance.GetOrCreateProperty<string>($"vr.object.{objectKeyName}.grabbingController");
        }
        
        protected virtual void Start()
        {
            // Subscribe to VR hover events. Store the subscriptions for later disposal.
            _hoverEnterSub = EventBus.Subscribe<VRUIHoverEnterEvent>(OnHoverEnter);
            _hoverExitSub = EventBus.Subscribe<VRUIHoverExitEvent>(OnHoverExit);
        }
        
        protected override void OnDestroy()
        {
            // --- Crucial Cleanup ---
            // Dispose of the event subscriptions to prevent memory leaks.
            _hoverEnterSub?.Dispose();
            _hoverExitSub?.Dispose();

            // Ensure the object is released if it's destroyed while being held.
            if (_isGrabbedProp.Value)
            {
                ReleaseObject();
            }
            base.OnDestroy();
        }

        /// <summary>
        /// This method is called by an interaction system (like the one in FluxVRPlayer)
        /// when a controller's raycast hits this object and the trigger is pressed.
        /// </summary>
        public void OnVRInteract(FluxVRController controller)
        {
            if (!canBeGrabbed) return;
            
            if (!_isGrabbedProp.Value)
            {
                GrabObject(controller);
            }
            else if (_currentGrabbingController == controller)
            {
                ReleaseObject();
            }
        }
        
        private void GrabObject(FluxVRController controller)
        {
            _isGrabbedProp.Value = true;
            _currentGrabbingController = controller;
            _grabbingControllerProp.Value = controller.ControllerNode.ToString();
            
            if (_rigidbody != null)
            {
                _wasKinematic = _rigidbody.isKinematic;
                _rigidbody.isKinematic = true;
            }
            
            transform.SetParent(controller.transform);
            
            controller.TriggerHapticFeedback(0.5f, 0.1f);
            PublishEvent(new VRObjectGrabbedEvent(controller.ControllerNode, gameObject, transform.position));
            Debug.Log($"Grabbed {gameObject.name} with {controller.ControllerNode}");
        }
        
        private void ReleaseObject()
        {
            if (!_isGrabbedProp.Value || _currentGrabbingController == null) return;
            
            var controller = _currentGrabbingController;
            Vector3 releaseVelocity = Vector3.zero;
            Vector3 releaseAngularVelocity = Vector3.zero;
            
            if (canBeThrown)
            {
                releaseVelocity = controller.GetVelocity();
                releaseAngularVelocity = controller.GetAngularVelocity();
            }
            
            transform.SetParent(_originalParent);
            
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _wasKinematic;
                _rigidbody.linearVelocity = releaseVelocity;
                _rigidbody.angularVelocity = releaseAngularVelocity;
            }
            
            // Reset state
            _isGrabbedProp.Value = false;
            _grabbingControllerProp.Value = "";
            _currentGrabbingController = null;
            
            controller.TriggerHapticFeedback(0.3f, 0.05f);
            PublishEvent(new VRObjectReleasedEvent(controller.ControllerNode, gameObject, releaseVelocity));
            Debug.Log($"Released {gameObject.name}");
        }
        
        private void OnHoverEnter(VRUIHoverEnterEvent evt)
        {
            if (evt.UIElement == gameObject)
            {
                _isHoveredProp.Value = true;
                if(showOutlineOnHover) ShowOutline(true);
            }
        }
        
        private void OnHoverExit(VRUIHoverExitEvent evt)
        {
            if (evt.UIElement == gameObject)
            {
                _isHoveredProp.Value = false;
                if(showOutlineOnHover) ShowOutline(false);
            }
        }
        
        private void ShowOutline(bool show)
        {
            if (objectRenderer == null) return;
            
            if (show && outlineMaterial != null)
            {
                objectRenderer.material = outlineMaterial;
            }
            else if (_originalMaterial != null)
            {
                objectRenderer.material = _originalMaterial;
            }
        }
        
        private void OnDisable()
        {
            // If the object is disabled while grabbed, force a release.
            if (_isGrabbedProp.Value)
            {
                ReleaseObject();
            }
        }
    }
}