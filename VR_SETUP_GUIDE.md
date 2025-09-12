# 🥽 FluxFramework VR - Quick Setup Guide

This guide provides the fastest way to get a fully functional, reactive VR player rig running in your FluxFramework project.

## ⚡ Automatic VR Player Creation

The easiest way to get started is to let the framework build the VR player rig for you.

1.  Create an empty GameObject in your scene (e.g., named "[VR Rig Parent]").
2.  Add the **`FluxVRPlayerPrefab`** component to it.
3.  Ensure **"Create At Start"** is checked in the inspector.
4.  Press Play.

A complete VR rig, including a manager, controllers, and locomotion, will be created as a child of this object.

### Recommended Hierarchy (What gets created)
```
[VR Rig Parent] (your object)
└── FluxVR Player (Root)
    ├── CharacterController
    ├── FluxVRManager
    ├── FluxVRLocomotion
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

Listen to the globally available reactive properties using the safe lifecycle methods.

```csharp
using System; // Required for IDisposable
using FluxFramework.Core;

public class MyVRArm : FluxMonoBehaviour
{
    private IDisposable _subscription;

    // Use OnFluxAwake() for safe initialization and subscriptions.
    // It's guaranteed to run after the framework is ready.
    protected override void OnFluxAwake()
    {
        var rightHandPosProp = FluxManager.Instance.GetOrCreateProperty<Vector3>("vr.controller.right.position");
        
        // Subscribe and fire immediately to get the initial position.
        _subscription = rightHandPosProp.Subscribe(newPosition =>
        {
            transform.position = newPosition;
        }, fireOnSubscribe: true);
    }
    
    // Use OnFluxDestroy() to clean up subscriptions.
    protected override void OnFluxDestroy()
    {
        _subscription?.Dispose();
    }
}
```

### 2. Listening to VR Events

Use the `[FluxEventHandler]` attribute to react to high-level interactions without manual subscription.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR; // Required for FluxVRManager
using FluxFramework.VR.Events; // Required for VR event types

public class VRGameManager : FluxMonoBehaviour
{
    // The framework automatically subscribes and unsubscribes this method.
    [FluxEventHandler]
    private void OnObjectGrabbed(VRObjectGrabbedEvent evt)
    {
        Debug.Log($"An object was grabbed: {evt.GrabbedObject.name}");

        // Example: Trigger haptics on the controller that grabbed the object.
        var vrManager = FindObjectOfType<FluxVRManager>();
        var controller = vrManager?.GetController(evt.ControllerNode);
        controller?.TriggerHapticFeedback(0.7f, 0.1f);
    }
}
```

### 3. Creating a VR-Interactable Object

Create an object that the player can interact with using the laser pointer.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VR; // Required for FluxVRController and IVRInteractable

// Attach this script to any GameObject with a Collider.
public class MySimpleButton : FluxMonoBehaviour, IVRInteractable
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
using UnityEngine;
using FluxFramework.UI;
using FluxFramework.VR.UI; // Required for FluxVRText
using FluxFramework.Attributes;

public class MyVRDashboard : FluxUIComponent
{
    // The [FluxBinding] attribute on a component reference is all you need.
    // The base FluxUIComponent handles the registration automatically.
    [FluxBinding("vr.hmd.position")]
    [SerializeField] private FluxVRText _hmdPositionText;

    [FluxBinding("inventory.gold")]
    [SerializeField] private FluxVRText _goldText;
}
```

## 🎮 Default Controls

-   **Left Thumbstick:** Smooth Movement (if enabled in `FluxVRLocomotion`).
-   **Right Thumbstick (Up):** Aim Teleportation.
-   **Right Thumbstick (Release from Up):** Execute Teleport.
-   **Right Thumbstick (Left/Right):** Snap/Smooth Turning.
-   **Trigger (while pointing at UI):** Clicks a UI element.
-   **Trigger (while pointing at `IVRInteractable`):** Interacts with the object.

## 🐛 Troubleshooting

-   **"UI is not responding to clicks"**: Ensure your `Canvas` has a `Graphic Raycaster` and your scene has an `EventSystem`. For VR, you may also need an `XRUIInputModule` on your `EventSystem` if using Unity's XR Interaction Toolkit.
-   **"Teleportation doesn't work"**: Check the `Teleport Layer Mask` on the `FluxVRLocomotion` component to ensure it includes your ground/platform layers.
-   **"No controllers appear"**: Make sure your headset is properly connected and that your chosen provider is enabled in `Project Settings` → `XR Plug-in Management`.

## 🚀 Ready for VR!

Your FluxFramework project is now VR-ready! 🥽