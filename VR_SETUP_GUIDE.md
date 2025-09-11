# 🥽 FluxFramework VR - Quick Setup Guide

This guide provides the fastest way to get a fully functional, reactive VR player rig running in your FluxFramework project.

## ⚡ Automatic VR Player Creation

The easiest way to get started is to let the framework build the VR player rig for you.

1.  Create an empty GameObject in your scene (e.g., named "[VR]").
2.  Add the **`FluxVRPlayerPrefab`** component to it.
3.  Ensure **"Create At Start"** is checked in the inspector.
4.  Press Play.

A complete VR rig, including a manager, controllers, and locomotion, will be created as a child of this object.

### Recommended Hierarchy (What gets created)
```
[VR] (your object)
└── FluxVR Player (Root)
    ├── CharacterController
    ├── FluxVRManager
    ├── FluxVRLocomoion
    ├── FluxVRPlayer (the "brain")
    └── Camera Offset
        ├── Main Camera (HMD)
        │   ├── Camera
        │   └── AudioListener
        └── (Controllers will be spawned here at runtime)
```

## 🔧 Required Configuration

### 1. Unity's XR Plug-in Management
- Go to `Edit` → `Project Settings` → `XR Plug-in Management`.
- Ensure you have a plug-in provider enabled for your target platform (e.g., OpenXR, Oculus).

### 2. Scene Setup
- Ensure your scene has an **`EventSystem`**. If not, right-click in the Hierarchy → `UI` → `Event System`.

---

## 🎯 Immediate Usage Examples

Once the VR player is in your scene, you can immediately start interacting with it from any other `FluxMonoBehaviour` script.

### 1. Reacting to VR Controller State

Listen to the globally available reactive properties.

```csharp
public class MyVRArm : FluxMonoBehaviour
{
    private IDisposable _subscription;

    protected override void Awake()
    {
        base.Awake();
        
        // Get the reactive property for the right controller's position
        var rightHandPosProp = FluxManager.Instance.GetOrCreateProperty<Vector3>("vr.controller.right.position");
        
        // Subscribe to its changes to make this arm follow the controller
        _subscription = rightHandPosProp.Subscribe(newPosition =>
        {
            transform.position = newPosition;
        });
    }
    
    protected override void OnDestroy()
    {
        _subscription?.Dispose(); // Always clean up subscriptions!
        base.OnDestroy();
    }
}
```

### 2. Listening to VR Events

Use the `[FluxEventHandler]` attribute to react to high-level interactions.

```csharp
public class VRGameManager : FluxMonoBehaviour
{
    [FluxEventHandler]
    private void OnObjectGrabbed(VRObjectGrabbedEvent evt)
    {
        Debug.Log($"An object was grabbed: {evt.GrabbedObject.name}");
        // Add points to the score, trigger a sound effect, etc.
    }

    [FluxEventHandler]
    private void OnPlayerTeleported(VRTeleportEvent evt)
    {
        Debug.Log($"Player teleported to {evt.ToPosition}");
        // Trigger a teleport particle effect.
    }
}
```

### 3. Creating a VR-Interactable Object

Create an object that the player can interact with using the laser pointer.

```csharp
// Attach this script to any GameObject with a Collider.
public class MyVRButton : FluxMonoBehaviour, IVRInteractable
{
    // This method will be called automatically by the FluxVRPlayer's
    // interaction system when a controller points at this object and clicks the trigger.
    public void OnVRInteract(FluxVRController controller)
    {
        Debug.Log($"This button was pressed by the {controller.ControllerNode} controller!");
        
        // Add your button's logic here.
        // e.g., Open a door, change a color, etc.
    }
}
```

### 4. Binding a VR UI

The process is identical to binding a 2D UI. The UI will automatically update.

```csharp
public class MyVRDashboard : FluxUIComponent
{
    // The [FluxBinding] attribute on a component reference is all you need.
    [FluxBinding("vr.hmd.position")]
    [SerializeField] private FluxVRText _hmdPositionText;

    [FluxBinding("inventory.gold")]
    [SerializeField] private FluxVRText _goldText;
}
```

## 🎮 Default Controls

-   **Left Thumbstick:** Smooth Movement (if enabled in `FluxVRLocomotion`).
-   **Right Thumbstick (Up):** Aim Teleportation.
-   **Right Thumbstick (Release):** Execute Teleport.
-   **Right Thumbstick (Left/Right):** Snap/Smooth Turning.
-   **Trigger (while pointing at UI):** Clicks a UI element.
-   **Trigger (while pointing at `IVRInteractable`):** Interacts with the object.

## 🐛 Troubleshooting

-   **"UI is not responding to clicks"**: Ensure your `Canvas` has a `Graphic Raycaster` and your scene has an `EventSystem`.
-   **"Teleportation doesn't work"**: Check the `Teleport Layer Mask` on the `FluxVRLocomotion` component to ensure it includes your ground/platform layers.
-   **"No controllers appear"**: Make sure your headset is properly connected and that your chosen provider is enabled in `XR Plug-in Management`.

## 🚀 Ready for VR!

Your FluxFramework project is now VR-ready! 🥽