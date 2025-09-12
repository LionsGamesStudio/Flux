# 🚀 FluxFramework

**An innovative Unity framework for decoupling logic and UI with a modern, reactive architecture.**

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.2.0-orange.svg)](package.json)

FluxFramework provides a powerful, attribute-driven workflow to build scalable and maintainable applications in Unity. It enforces a clean separation between your game's state/logic and its presentation layer, eliminating "spaghetti code" and making your project easier to debug, test, and expand.

## ✨ Core Features

### 🔄 **Centralized Reactive State**
- **Reactive Properties:** A global, thread-safe state manager for your application's data.
- **Automatic Discovery:** Declare reactive state directly in your components with a `[ReactiveProperty]` attribute. The framework handles the registration.
- **Advanced Capabilities:** Automatic validation (`[FluxRange]`), reactive collections, and LINQ-style extension methods (`Transform`, `CombineWith`).

### 🎯 **Declarative UI Data Binding**
- **Zero Code Binding:** Bind UI components to data with a single `[FluxBinding]` attribute in the inspector.
- **Rich Component Set:** Includes generic, configurable components like `FluxText`, `FluxImage`, `FluxSlider`, and `FluxToggle`.
- **Powerful Binding Options:** Full support for `OneWay` & `TwoWay` binding, automatic and custom **Value Converters**.

### ⚡ **Decoupled Event System**
- **Global EventBus:** A thread-safe, high-performance message bus for system-wide communication.
- **Attribute-Based Subscriptions:** Subscribe to any `FluxEvent` or `ReactiveProperty` change with a single attribute (`[FluxEventHandler]`, `[FluxPropertyChangeHandler]`).
- **Automatic Lifecycle Management:** The framework handles subscription and unsubscription automatically, preventing memory leaks.

### 🛠️ **Superior Developer Experience**
- **Rich Editor Tooling:** A full suite of editor windows, including a Visual Scripting Editor, a live Reactive Properties Inspector, and an Event Bus Monitor.
- **Intelligent Inspectors:** Custom editors provide a clean, feature-rich experience with `[FluxGroup]`, `[FluxButton]`, and `[FluxAction]` for in-editor debugging.
- **Code Generation:** Built-in script templates to create perfectly structured framework classes in seconds.

### 🎨 **Integrated Visual Scripting**
- A powerful node-based editor for creating game logic without writing code, fully integrated with the framework's core systems.
- *See [VISUAL_SCRIPTING_README.md](VISUAL_SCRIPTING_README.md) for complete documentation.*

### 🥽 **Complete VR Extension**
- A reactive XR player rig where controller inputs and HMD tracking are exposed as reactive properties and events.
- *See [VR_README.md](VR_README.md) for complete documentation.*

## 🚀 Installation

### Using Unity Package Manager
1. In Unity, open `Window` > `Package Manager`.
2. Click the `+` icon > `Add package from git URL...`
3. Enter the repository URL.

*(Alternatively, add the package from a local disk folder.)*

## 🎯 Quick Start Guide

### 1. The Safe Lifecycle: `FluxMonoBehaviour`

Inherit from `FluxMonoBehaviour` to integrate your components into the framework. It provides a **safe lifecycle**, guaranteeing that your code runs only after the framework is initialized.

```csharp
using FluxFramework.Core;

public class MyGameManager : FluxMonoBehaviour
{
    // Use OnFluxAwake() instead of Awake() for setup and subscriptions.
    protected override void OnFluxAwake()
    {
        Debug.Log("Framework is ready. Initializing my manager.");
    }

    // Use OnFluxStart() instead of Start() for logic that depends on other components.
    protected override void OnFluxStart()
    {
        Debug.Log("All components have run OnFluxAwake. Safe to interact now.");
    }
    
    // Use OnFluxDestroy() for cleanup.
    protected override void OnFluxDestroy()
    {
        Debug.Log("Cleaning up my manager.");
    }
}
```
**Key Concept:** The standard Unity methods (`Awake`, `Start`, `OnDestroy`) are sealed in the base class to prevent initialization order issues. **Always use the `OnFlux...` methods.**

### 2. Defining and Modifying State

Declare your state with the `[ReactiveProperty]` attribute. To change the state, you must explicitly update the property through the framework.

```csharp
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerStats : FluxMonoBehaviour
{
    // The attribute declares the property and its initial value.
    // The private field serves as a convenient, read-only cache.
    [ReactiveProperty("player.health")]
    private float _health = 100f;

    [FluxButton("Take 10 Damage")]
    private void TakeDamage()
    {
        // To MODIFY the state, use the helper method or the FluxManager directly.
        // This ensures all subscribers are notified.
        UpdateReactiveProperty("player.health", _health - 10f);
    }
}
```

### 3. Binding UI to State (Zero Code)

The `FluxUIComponent` base class handles all binding automatically.

```csharp
using UnityEngine;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;

public class PlayerHUD : FluxUIComponent
{
    // 1. Reference your UI element.
    // 2. Add the [FluxBinding] attribute to it.
    // 3. Set the Property Key.
    [Header("Bindings")]
    [FluxBinding("player.health")]
    [SerializeField] private TextMeshProUGUI _healthText;

    // That's it! The base class handles the rest. No binding code needed.
}
```

### 4. Reacting to State Changes and Events (Declarative)

Use attributes to turn methods into automatic listeners.

```csharp
using FluxFramework.Core;
using FluxFramework.Attributes;

public class AudioFeedback : FluxMonoBehaviour
{
    // This method is now automatically subscribed to the "player.health" property.
    // It will be called every time its value changes.
    [FluxPropertyChangeHandler("player.health")]
    private void OnHealthChanged(float newHealth)
    {
        if (newHealth < 25f)
        {
            // Play a low-health heartbeat sound.
        }
    }
    
    // This method is automatically subscribed to the PlayerJumpedEvent.
    [FluxEventHandler]
    private void OnPlayerJump(PlayerJumpedEvent evt)
    {
        // Play jump sound effect.
    }
}
```

## 🛠️ Best Practices

*   **Logic Creates State:** Use `FluxMonoBehaviour` or `FluxDataContainer` to define and manage your game's state with `[ReactiveProperty]`.
*   **Views Display State:** Use `FluxUIComponent` and `[FluxBinding]` to display data. UI components should contain minimal or no logic.
*   **Systems Communicate via Events:** Use `PublishEvent()` and `[FluxEventHandler]` for decoupled communication between different logic systems.
*   **Use the Safe Lifecycle:** Always use `OnFluxAwake`, `OnFluxStart`, and `OnFluxDestroy` for your component logic.

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.