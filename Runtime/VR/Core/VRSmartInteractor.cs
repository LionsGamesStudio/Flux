#if FLUX_VR_SUPPORT
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VR.Locomotion;
using FluxFramework.VR.Events;
using System;

namespace FluxFramework.VR
{
    /// <summary>
    /// An intelligent interactor that manages a raycast from the controller to interact with the world.
    /// It dynamically switches between targeting interactable objects and valid teleport surfaces based on priority.
    /// This component is also the designated owner of the controller's visual ray (LineRenderer).
    /// </summary>
    public class VRSmartInteractor : FluxMonoBehaviour
    {
        #region Inspector Fields
        [Header("Interaction Priority")]
        [Tooltip("If true, interactable objects will be targeted even if they are in front of a teleport surface. If false, teleport surfaces always take priority.")]
        [SerializeField] private bool prioritizeObjectInteraction = true;
        
        [Header("References")]
        [Tooltip("Reference to the controller that owns this interactor. Usually assigned at runtime.")]
        [SerializeField] private FluxVRController controller;
        [Tooltip("Reference to the rig's locomotion system. Injected at runtime by the VR Manager.")]
        [SerializeField] private FluxVRLocomotion locomotionSystem;
        [Tooltip("The LineRenderer used to draw the interaction ray. Will be auto-discovered in children if not assigned.")]
        [SerializeField] private LineRenderer rayRenderer;
        
        [Header("Ray Settings")]
        [Tooltip("The maximum distance the interaction ray can travel.")]
        [SerializeField] private float rayMaxDistance = 10f;
        [Tooltip("The physics layers that the interaction ray is allowed to hit.")]
        [SerializeField] private LayerMask interactionLayerMask = -1;
        
        [Header("Visual Feedback")]
        [Tooltip("The color of the ray when pointing at a standard interactable object.")]
        [SerializeField] private Color interactableColor = Color.cyan;
        [Tooltip("The color of the ray when pointing at a valid teleport location.")]
        [SerializeField] private Color teleportColor = Color.green;
        [Tooltip("The color of the ray when pointing at a non-interactive surface.")]
        [SerializeField] private Color invalidColor = Color.red;
        #endregion

        #region Private State
        private InteractionMode _currentMode = InteractionMode.None;
        private IVRInteractable _currentInteractable;
        private IVRHoverable _currentHoverable;
        private VRTeleportSurface _currentTeleportSurface;

        // A handle to the event subscription for safe cleanup on destroy.
        private IDisposable _triggerPressedSub;
        #endregion

        /// <summary>
        /// Initializes the interactor with its required dependencies. This method is called by the FluxVRManager
        /// after the controller has been instantiated.
        /// </summary>
        /// <param name="ownerController">The controller this interactor belongs to.</param>
        /// <param name="locomotion">The main VR rig's locomotion system.</param>
        public void Initialize(FluxVRController ownerController, FluxVRLocomotion locomotion)
        {
            controller = ownerController;
            locomotionSystem = locomotion;
            
            // Attempt to find the LineRenderer in this component's children if it's not set.
            if (rayRenderer == null)
            {
                rayRenderer = GetComponentInChildren<LineRenderer>();
            }

            if (rayRenderer != null)
            {
                SetupRayRenderer();
            }
            else
            {
                Flux.Manager.Logger.Warning("VRSmartInteractor could not find a LineRenderer in its children. The interaction ray will be invisible.", this);
            }
        }

        protected override void OnFluxStart()
        {
            base.OnFluxStart();
            // Subscribe to the trigger press event to handle interactions in an event-driven way.
            _triggerPressedSub = Flux.Manager.EventBus.Subscribe<VRTriggerPressedEvent>(OnTriggerPressed);
        }

        protected override void OnFluxDestroy()
        {
            base.OnFluxDestroy();
            // Unsubscribe from the event to prevent memory leaks when the controller is destroyed.
            _triggerPressedSub?.Dispose();
        }

        /// <summary>
        /// Configures the initial state of the LineRenderer.
        /// </summary>
        private void SetupRayRenderer()
        {
            if (rayRenderer == null) return;
            // The interactor now takes responsibility for enabling its own ray.
            rayRenderer.enabled = true;
        }
        
        protected virtual void Update()
        {
            UpdateRaycast();
        }
        
        /// <summary>
        /// Performs a raycast every frame to determine what the controller is pointing at.
        /// Manages the visual state of the ray and updates the current interaction mode.
        /// </summary>
        private void UpdateRaycast()
        {
            if (controller == null || rayRenderer == null)
            {
                if(rayRenderer != null) rayRenderer.enabled = false;
                return;
            }

            rayRenderer.enabled = true;
            
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = transform.forward;
            
            rayRenderer.SetPosition(0, rayOrigin);
            
            bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, rayMaxDistance, interactionLayerMask);
            
            if (hitSomething)
            {
                rayRenderer.SetPosition(1, hit.point);
                
                var interactable = hit.collider.GetComponent<IVRInteractable>();
                var hoverable = hit.collider.GetComponent<IVRHoverable>();
                var teleportSurface = hit.collider.GetComponent<VRTeleportSurface>();
                
                if (prioritizeObjectInteraction)
                {
                    if (interactable != null) SetMode(InteractionMode.ObjectInteraction, interactable, hoverable, null);
                    else if (teleportSurface != null && teleportSurface.IsEnabled) SetMode(InteractionMode.Teleportation, null, null, teleportSurface);
                    else SetMode(InteractionMode.Invalid, null, null, null);
                }
                else
                {
                    if (teleportSurface != null && teleportSurface.IsEnabled) SetMode(InteractionMode.Teleportation, null, null, teleportSurface);
                    else if (interactable != null) SetMode(InteractionMode.ObjectInteraction, interactable, hoverable, null);
                    else SetMode(InteractionMode.Invalid, null, null, null);
                }
            }
            else
            {
                rayRenderer.SetPosition(1, rayOrigin + rayDirection * rayMaxDistance);
                SetMode(InteractionMode.None, null, null, null);
            }
            
            UpdateRayColor();
        }
        
        /// <summary>
        /// Manages the state transition between different interaction modes (e.g., from hovering an object
        /// to hovering a teleport surface). It ensures that OnHoverEnter and OnHoverExit events are called correctly.
        /// </summary>
        private void SetMode(InteractionMode newMode, IVRInteractable interactable, IVRHoverable hoverable, VRTeleportSurface teleportSurface)
        {
            // Notify the previous hoverable object that it is no longer being hovered.
            if (_currentHoverable != null && hoverable != _currentHoverable) _currentHoverable.OnHoverExit(controller);
            if (_currentTeleportSurface != null && teleportSurface != _currentTeleportSurface) _currentTeleportSurface.OnTargetExit();
            
            // Update the current state.
            _currentMode = newMode;
            _currentInteractable = interactable;
            _currentHoverable = hoverable;
            _currentTeleportSurface = teleportSurface;
            
            // Notify the new hoverable object that it is now being hovered.
            if (_currentHoverable != null) _currentHoverable.OnHoverEnter(controller);
            if (_currentTeleportSurface != null) _currentTeleportSurface.OnTargetEnter();
        }
        
        /// <summary>
        /// Updates the color of the LineRenderer based on the current interaction mode.
        /// </summary>
        private void UpdateRayColor()
        {
            if (rayRenderer == null) return;
            
            Color color = _currentMode switch
            {
                InteractionMode.ObjectInteraction => interactableColor,
                InteractionMode.Teleportation => _currentTeleportSurface != null ? _currentTeleportSurface.MarkerColor : teleportColor,
                InteractionMode.Invalid => invalidColor,
                _ => Color.white
            };
            
            rayRenderer.startColor = color;
            rayRenderer.endColor = new Color(color.r, color.g, color.b, 0f); // Fade to transparent
        }
        
        /// <summary>
        /// Event handler that is called when the controller's trigger is pressed.
        /// It performs the appropriate action based on the current interaction mode.
        /// </summary>
        private void OnTriggerPressed(VRTriggerPressedEvent evt)
        {
            // Ensure the event is for this specific controller before acting.
            if (controller == null || evt.ControllerNode != controller.ControllerNode) return;

            switch (_currentMode)
            {
                case InteractionMode.ObjectInteraction:
                    if (_currentInteractable != null)
                    {
                        _currentInteractable.OnVRInteract(controller);
                    }
                    break;
                    
                case InteractionMode.Teleportation:
                    if (_currentTeleportSurface != null && locomotionSystem != null)
                    {
                        // The hit point is the end of the rendered ray.
                        Vector3 targetPosition = rayRenderer.GetPosition(1);
                        locomotionSystem.TeleportToPosition(targetPosition);
                    }
                    break;
            }
        }
        
        public InteractionMode CurrentMode => _currentMode;
        
        public void SetRayEnabled(bool enabled)
        {
            if (rayRenderer != null)
            {
                rayRenderer.enabled = enabled;
            }
        }
    }
    
    public enum InteractionMode
    {
        None,
        ObjectInteraction,
        Teleportation,
        Invalid
    }
}
#endif