# 🥽 FluxFramework VR Extension

**Seamlessly extend your reactive FluxFramework projects into immersive Virtual Reality experiences.**

This extension builds on the core principles of FluxFramework, exposing VR hardware state (HMD, controllers) and interactions as reactive properties and events, enabling a fully decoupled VR architecture.

## ✨ VR Features

### 🎮 **Centralized VR Management**
- **`FluxVRManager`:** A central hub that automatically discovers and manages the lifecycle of XR devices.
- **Reactive Tracking:** Real-time HMD and controller tracking data (position, rotation, velocity) exposed as global reactive properties.
- **Device Hot-Swapping:** Automatically detects controller connections and disconnections at runtime.

### 🕹️ **Reactive VR Input**
- **`FluxVRController`:** A component that translates raw controller inputs (buttons, triggers, thumbsticks) into reactive properties and a rich set of specific VR events.
- **Integrated Haptic Feedback:** A simple, unified API to trigger haptic feedback on any controller.

### 🖼️ **Spatial UI System**
- **`FluxVRCanvas`:** A world-space canvas optimized for VR, with options for HMD-following ("head-locked") UI.
- **`VRUIInteractor`:** A laser-pointer-based interaction system that seamlessly integrates with Unity's Event System, making standard UI components (Buttons, Sliders) work in VR out-of-the-box.
- **VR-Optimized Components:** `FluxVRButton` and `FluxVRText` provide enhanced visual feedback and functionality for VR environments.

### 🚶 **Flexible Locomotion**
- A complete locomotion system (`FluxVRLocomotion`) with built-in **Teleportation** (with destination validation) and **Smooth Movement**.
- Configurable **Snap/Smooth Turning** options for user comfort.
- Built-in comfort features like screen fading on teleport.

---

## 🚀 Quick Setup

1.  Ensure you have the core `FluxFramework` package and `com.unity.xr.management` installed.
2.  Import the VR extension package into your project.
3.  Create an empty GameObject in your scene named "**VR Rig**".
4.  Attach the **`FluxVRPlayerPrefab`** component to it. This component will programmatically build a complete, ready-to-use player rig at startup.
5.  Press Play. 🎉

---

## 🎯 Quick Usage Examples

The core principle is to **react** to the state changes and events provided by the VR system.

### 1. Reacting to Controller Input (Property Subscription)

Your game logic doesn't need to know about XR devices. It just subscribes to a property using its type-safe key.

```csharp
// In a WeaponController.cs script
using System;
using FluxFramework.Core;
using Flux; // <-- Import for Flux and FluxKeys

public class WeaponController : FluxMonoBehaviour
{
    private IDisposable _triggerSubscription;

    // Use OnFluxAwake for safe initialization and subscriptions.
    protected override void OnFluxAwake()
    {
        // 1. Get the property using the clean, type-safe API.
        // 2. Subscribe and store the IDisposable handle.
        _triggerSubscription = SubscribeToProperty<float>(FluxKeys.VrControllerLeftTrigger, OnTriggerChanged);
    }
    
    // The base FluxMonoBehaviour handles cleanup, but manual disposal is good practice for clarity.
    protected override void OnFluxDestroy()
    {
        _triggerSubscription?.Dispose();
    }
    
    private void OnTriggerChanged(float value)
    {
        if (value > 0.8f)
        {
            FireWeapon();
        }
    }

    private void FireWeapon() { /* ... */ }
}
```

### 2. Listening to VR Interaction Events (Event Handling)

Use the `[FluxEventHandler]` attribute for a code-free way to react to high-level VR events.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class InteractionFeedback : FluxMonoBehaviour
{
    // This method is automatically subscribed to the VRObjectGrabbedEvent.
    // The framework handles the cleanup automatically.
    [FluxEventHandler]
    private void OnObjectGrabbed(VRObjectGrabbedEvent evt)
    {
        Debug.Log($"Player grabbed {evt.GrabbedObject.name} with their {evt.ControllerNode}!");
        
        // Find the VR Manager to get the controller and trigger haptics.
        // Note: In a larger project, a dedicated service or manager would be better than FindObjectOfType.
        var vrManager = FindObjectOfType<FluxVRManager>();
        var controller = vrManager?.GetController(evt.ControllerNode);
        controller?.TriggerHapticFeedback(0.7f, 0.1f);
    }
}
```

### 3. Binding VR UI to Player Data (UI Binding)

Binding a VR UI is identical to binding a 2D UI. The components are fully compatible and the UI will update automatically.

```csharp
using UnityEngine;
using FluxFramework.UI;
using FluxFramework.Attributes;
using Flux; // <-- Import for FluxKeys

// On your FluxVRCanvas, you might have a PlayerStatusPanel.cs
public class PlayerStatusPanel : FluxUIComponent
{
    // This declarative attribute is all you need.
    // When the "player.health" property changes, this VR text component will update automatically.
    [FluxBinding(FluxKeys.PlayerHealth)]
    [SerializeField] private FluxVRText _healthText;
}
```

## 🎨 Available VR Components

### Core VR
-   `FluxVRManager`: The central hub for the VR rig.
-   `FluxVRController`: Represents a physical controller, providing reactive inputs.
-   `FluxVRLocomotion`: Handles player movement (Teleport/Smooth).
-   `FluxVRPlayer`: The "brain" that coordinates systems and handles world interaction.
-   `FluxVRPlayerPrefab`: A factory for building a complete VR rig.

### VR Interaction Examples
-   `VRInteractableObject`: An example of a simple grabbable object.
-   `VRInteractiveButton`: An example of a physically pressable button.
-   `IVRInteractable`: An interface for creating your own interactable objects.

### VR UI
-   `FluxVRCanvas`: The container for a world-space UI.
-   `VRUIInteractor`: The laser pointer component attached to controllers.
-   `FluxVRButton`: A UI button with enhanced visual feedback for VR.
-   `FluxVRText`: A UI text component with VR-specific features like billboarding.

## 🔗 Integration with Core Framework

The VR extension is a perfect example of building on top of the core framework:

-   ✅ **Reactive Properties:** All hardware states (tracking, input) are exposed as standard reactive properties, accessible via the type-safe `FluxKeys` class.
-   ✅ **Event Bus:** All high-level actions (teleporting, grabbing, UI clicks) are published as standard `FluxEvent`s, allowing any system to listen and react without direct dependencies.
-   ✅ **UI Binding:** VR UI components are `FluxUIComponent`s, meaning they work seamlessly with the automatic `[FluxBinding]` system.

This creates a powerful, unified development experience, whether you are building for 2D, 3D, or VR.