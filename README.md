# 🚀 FluxFramework

**An innovative Unity framework for decoupling logic and UI with a modern, reactive architecture.**

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.1.0-orange.svg)](package.json)

FluxFramework provides a powerful, attribute-driven workflow to build scalable and maintainable applications in Unity. It enforces a clean separation between your game's state/logic and its presentation layer, eliminating "spaghetti code" and making your project easier to debug, test, and expand.

## ✨ Core Features

### 🔄 **Centralized Reactive State**
- **Reactive Properties:** Globally accessible, type-safe properties that automatically notify subscribers of changes.
- **Thread Safety:** Core managers are thread-safe, allowing seamless work with asynchronous operations.
- **Advanced Capabilities:** Built-in validation, reactive collections, and LINQ-style extension methods (`Transform`, `CombineWith`, `Where`).

### 🎯 **Declarative UI Data Binding**
- **Attribute-Driven:** Bind UI elements to data with a single `[FluxBinding]` attribute in the inspector. No boilerplate code required.
- **Rich Component Set:** Includes `FluxText`, `FluxImage`, `FluxSlider`, `FluxToggle`, and more.
- **Powerful Binding Options:** Full support for `OneWay`, `TwoWay` binding, value converters, and update debouncing.

### ⚡ **Decoupled Event System**
- **Global EventBus:** A thread-safe, high-performance message bus for system-wide communication.
- **Attribute-Based Subscriptions:** Subscribe to any event by adding a `[FluxEventHandler]` attribute to a method.
- **Automatic Lifecycle Management:** The framework handles subscription and unsubscription automatically.

### 🛠️ **Superior Developer Experience**
- **Rich Editor Tooling:** A full suite of editor windows, including a Visual Scripting Editor, a live Reactive Properties Inspector, and an Event Bus Monitor.
- **Intelligent Inspectors:** Custom editors and property drawers provide a clean, intuitive, and feature-rich experience (`[FluxGroup]`, `[FluxButton]`, `[FluxAction]`).
- **Code Generation:** Built-in script templates to create perfectly structured framework classes in seconds.

### 🎨 **Integrated Visual Scripting**
- **Full-Featured Graph Editor:** A powerful node-based editor for creating game logic without writing code.
- **Seamless Framework Integration:** Nodes for directly manipulating Reactive Properties, publishing/listening to events, controlling UI bindings, and more.
- **Context-Aware & Safe:** The execution engine provides nodes with scene context, enabling robust, instance-based logic and preventing memory leaks.
- *See [VISUAL_SCRIPTING_README.md](VISUAL_SCRIPTING_README.md) for complete documentation.*

### 🥽 **Complete VR Extension**
- **Reactive XR Rig:** A full VR player rig where controller inputs and HMD tracking are exposed as reactive properties.
- **VR-Specific Components:** World-space canvases, interactable objects, and locomotion systems (teleport, smooth movement) that are fully integrated with the framework.
- *See [VR_README.md](VR_README.md) for complete documentation.*

## 🚀 Installation

### Using Unity Package Manager
1. In Unity, open `Window` > `Package Manager`.
2. Click the `+` icon > `Add package from git URL...`
3. Enter the repository URL: `[Your Git Repository URL]`
4. Click `Add`.

*(Alternatively, you can add it from a local disk folder.)*

## 🎯 Quick Start Guide

### 1. Understanding the Core Classes

FluxFramework provides base classes to integrate your scripts into its ecosystem.

- **`FluxMonoBehaviour`:** The base class for any **logic or controller** component. Inherit from this to create player controllers, game managers, system controllers, etc.
- **`FluxUIComponent`:** The base class for any **UI view** component. Inherit from this to create custom UI elements that support data binding.
- **`FluxDataContainer`:** A `ScriptableObject` base class for storing **pure data** (e.g., character stats, item definitions).

### 2. Creating a Reactive Property

Define your game's state using `[ReactiveProperty]` on fields within a `FluxMonoBehaviour` or `FluxScriptableObject`.

```csharp
// In a component like a GameManager.cs that inherits from FluxMonoBehaviour
using FluxFramework.Core;
using FluxFramework.Attributes;

public class GameState : FluxMonoBehaviour
{
    // This creates a global ReactiveProperty<int> with the key "game.score"
    // and initializes it with a value of 0.
    [ReactiveProperty("game.score")]
    private int _score = 0;

    [FluxButton("Add 100 Points")]
    private void AddScore()
    {
        // To modify the state, we get the property from the manager and set its .Value
        var scoreProp = FluxManager.Instance.GetProperty<int>("game.score");
        if (scoreProp != null)
        {
            scoreProp.Value += 100;
        }
    }
}
```

### 3. Binding UI to a Reactive Property

Link your UI to the data with **zero code**.

1.  Attach a `FluxText` component to a `TextMeshProUGUI` object in your scene.
2.  In the inspector, find the `_textBinding` field.
3.  Set the **Property Key** in its `[FluxBinding]` attribute to `"game.score"`.
4.  Optionally, set the `Format String` on the `FluxText` component to `"Score: {0}"`.

**That's it!** Now, whenever the `"game.score"` property changes, the text on screen will automatically update.

### 4. Handling Events

Listen for framework or game events using the `[FluxEventHandler]` attribute.

```csharp
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events; // Example for a VR event

public class AudioManager : FluxMonoBehaviour
{
    // This method will be automatically subscribed to the VRTriggerPressedEvent.
    [FluxEventHandler]
    private void OnVRTriggerPressed(VRTriggerPressedEvent evt)
    {
        // Play a sound only if it was the left hand controller
        if (evt.ControllerNode == UnityEngine.XR.XRNode.LeftHand)
        {
            Debug.Log("Playing sound for left trigger press!");
        }
    }
}
```

## 🛠️ Best Practices

*   **State in `FluxMonoBehaviour` or `FluxDataContainer`:** Define your `[ReactiveProperty]` fields in logic components or data assets, not in UI components.
*   **Views are Dumb:** `FluxUIComponent`s should only be responsible for displaying data. They should not contain game logic. Use `[FluxBinding]` to connect them to the state.
*   **Communicate with Events:** When one system needs to notify another system of an action (e.g., "Player took damage"), publish an event. Don't create direct references between controllers.
*   **Use the Editor Tools:** Leverage the Visual Scripting editor, Reactive Properties Inspector, and script templates to accelerate your development workflow.

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.