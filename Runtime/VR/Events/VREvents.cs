#if FLUX_VR_SUPPORT
using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;

namespace FluxFramework.VR.Events
{
    // --- BASE CLASS ---

    /// <summary>
    /// Base class for all VR-specific events, providing a common source identifier.
    /// </summary>
    public abstract class VREventBase : FluxEventBase
    {
        public VREventBase(string source = "VR") : base(source) { }
    }

    // --- SYSTEM EVENTS ---

    /// <summary>
    /// Published by FluxVRManager when the VR system is initialized.
    /// </summary>
    public class VRInitializedEvent : VREventBase
    {
        public bool IsVRActive { get; }
        public string DeviceName { get; }
        
        public VRInitializedEvent(bool isActive, string deviceName) : base("VRManager")
        {
            IsVRActive = isActive;
            DeviceName = deviceName;
        }
    }

    /// <summary>
    /// Published by FluxVRManager when a new VR controller is detected and initialized.
    /// </summary>
    public class VRControllerConnectedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public string DeviceName { get; }
        
        public VRControllerConnectedEvent(XRNode node, string deviceName) : base("VRManager")
        {
            ControllerNode = node;
            DeviceName = deviceName;
        }
    }

    /// <summary>
    /// Published by FluxVRManager when a VR controller is no longer detected.
    /// </summary>
    public class VRControllerDisconnectedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        
        public VRControllerDisconnectedEvent(XRNode node) : base("VRManager")
        {
            ControllerNode = node;
        }
    }
    
    // --- INPUT AXIS EVENTS ---

    /// <summary>
    /// Published when a controller's trigger is pressed past a certain threshold.
    /// Carries the analog value of the trigger.
    /// </summary>
    public class VRTriggerPressedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public float TriggerValue { get; }
        
        public VRTriggerPressedEvent(XRNode node, float value) : base("VRInput")
        {
            ControllerNode = node;
            TriggerValue = value;
        }
    }

    /// <summary>
    /// Published when a controller's trigger is released.
    /// </summary>
    public class VRTriggerReleasedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        
        public VRTriggerReleasedEvent(XRNode node) : base("VRInput")
        {
            ControllerNode = node;
        }
    }
    
    /// <summary>
    /// Published when a controller's grip is pressed past a certain threshold.
    /// Carries the analog value of the grip.
    /// </summary>
    public class VRGripPressedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public float GripValue { get; }
        
        public VRGripPressedEvent(XRNode node, float value) : base("VRInput")
        {
            ControllerNode = node;
            GripValue = value;
        }
    }

    /// <summary>
    /// Published when a controller's grip is released.
    /// </summary>
    public class VRGripReleasedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        
        public VRGripReleasedEvent(XRNode node) : base("VRInput")
        {
            ControllerNode = node;
        }
    }

    // --- INPUT BUTTON EVENTS (Consolidated) ---

    /// <summary>
    /// Published when any controller button is pressed down.
    /// </summary>
    public class VRButtonPressedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public VRButtonType ButtonType { get; }
        
        public VRButtonPressedEvent(XRNode node, VRButtonType buttonType) : base("VRInput")
        {
            ControllerNode = node;
            ButtonType = buttonType;
        }
    }

    /// <summary>
    /// Published when any controller button is released.
    /// </summary>
    public class VRButtonReleasedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public VRButtonType ButtonType { get; }
        
        public VRButtonReleasedEvent(XRNode node, VRButtonType buttonType) : base("VRInput")
        {
            ControllerNode = node;
            ButtonType = buttonType;
        }
    }

    // --- INTERACTION EVENTS ---

    /// <summary>
    /// Published when a VR controller grabs a compatible object.
    /// </summary>
    public class VRObjectGrabbedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public GameObject GrabbedObject { get; }
        public Vector3 GrabPoint { get; }
        
        public VRObjectGrabbedEvent(XRNode node, GameObject obj, Vector3 grabPoint) : base("VRInteraction")
        {
            ControllerNode = node;
            GrabbedObject = obj;
            GrabPoint = grabPoint;
        }
    }

    /// <summary>
    /// Published when a VR controller releases a grabbed object.
    /// </summary>
    public class VRObjectReleasedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public GameObject ReleasedObject { get; }
        public Vector3 ReleaseVelocity { get; }
        
        public VRObjectReleasedEvent(XRNode node, GameObject obj, Vector3 velocity) : base("VRInteraction")
        {
            ControllerNode = node;
            ReleasedObject = obj;
            ReleaseVelocity = velocity;
        }
    }

    // --- LOCOMOTION EVENTS ---

    /// <summary>
    /// Published by a locomotion system when the player is teleported.
    /// </summary>
    public class VRTeleportEvent : VREventBase
    {
        public Vector3 FromPosition { get; }
        public Vector3 ToPosition { get; }
        public XRNode ControllerUsed { get; } // Can be Head for programmatic teleports
        
        public VRTeleportEvent(Vector3 from, Vector3 to, XRNode controller) : base("VRLocomotion")
        {
            FromPosition = from;
            ToPosition = to;
            ControllerUsed = controller;
        }
    }

    // --- UI EVENTS ---

    /// <summary>
    /// Published by a VRUIInteractor when the user clicks on a UI element.
    /// </summary>
    public class VRUIClickEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public GameObject UIElement { get; }
        public Vector3 HitPoint { get; }
        
        public VRUIClickEvent(XRNode node, GameObject element, Vector3 hitPoint) : base("VRUI")
        {
            ControllerNode = node;
            UIElement = element;
            HitPoint = hitPoint;
        }
    }

    /// <summary>
    /// Published by a VRUIInteractor when a controller's pointer enters a UI element.
    /// </summary>
    public class VRUIHoverEnterEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public GameObject UIElement { get; }
        
        public VRUIHoverEnterEvent(XRNode node, GameObject element) : base("VRUI")
        {
            ControllerNode = node;
            UIElement = element;
        }
    }

    /// <summary>
    /// Published by a VRUIInteractor when a controller's pointer exits a UI element.
    /// </summary>
    public class VRUIHoverExitEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public GameObject UIElement { get; }
        
        public VRUIHoverExitEvent(XRNode node, GameObject element) : base("VRUI")
        {
            ControllerNode = node;
            UIElement = element;
        }
    }

    /// <summary>
    /// Published by a FluxVRCanvas when it gains or loses focus from an interactor.
    /// </summary>
    public class VRCanvasFocusChangedEvent : VREventBase
    {
        public GameObject Canvas { get; }
        public bool IsFocused { get; }
        
        public VRCanvasFocusChangedEvent(GameObject canvas, bool focused) : base("VRUI")
        {
            Canvas = canvas;
            IsFocused = focused;
        }
    }
    
    // --- GESTURE EVENTS ---

    /// <summary>
    /// A placeholder event for a future gesture recognition system.
    /// </summary>
    public class VRGestureDetectedEvent : VREventBase
    {
        public XRNode ControllerNode { get; }
        public VRGestureType GestureType { get; }
        public float Confidence { get; }
        
        public VRGestureDetectedEvent(XRNode node, VRGestureType gestureType, float confidence = 1f) : base("VRGesture")
        {
            ControllerNode = node;
            GestureType = gestureType;
            Confidence = confidence;
        }
    }

    public enum VRGestureType
    {
        Swipe,
        Point,
        Wave,
        Circle,
        Throw,
        Pull,
        Push,
        Custom
    }
}
#endif