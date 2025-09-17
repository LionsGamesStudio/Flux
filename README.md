# 🚀 FluxFramework

**An innovative Unity framework for decoupling logic and UI with a modern, reactive, and testable architecture.**

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-3.0.0-orange.svg)](package.json)
[![CI Status](https://github.com/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME/actions/workflows/main.yml/badge.svg)](https://github.com/YOUR_GITHUB_USERNAME/YOUR_REPO_NAME/actions/workflows/main.yml)

FluxFramework provides a powerful, attribute-driven workflow to build scalable and maintainable applications in Unity. It enforces a clean separation between your game's state/logic and its presentation layer, eliminating "spaghetti code" and making your project easier to debug, test, and expand.

## ✨ Core Features

### 🔄 **Centralized Reactive State**
- **Reactive Properties:** An instance-based, thread-safe state manager for your application's data, accessible via `Flux.Manager.Properties`.
- **Automatic Discovery:** Declare state directly in your components with a `[ReactiveProperty]` attribute. The framework handles registration and lifecycle.
- **Advanced Capabilities:** Built-in validation (`[FluxRange]`), reactive collections, and LINQ-style extension methods (`Transform`, `CombineWith`).

### 🎯 **Declarative UI Data Binding**
- **Zero-Code Binding:** Bind UI components to data with a single `[FluxBinding]` attribute in the inspector.
- **Rich Component Set:** Includes generic, configurable components like `FluxText`, `FluxImage`, `FluxSlider`, and `FluxToggle`.
- **Powerful Binding Options:** Full support for `OneWay` & `TwoWay` binding, with automatic and custom **Value Converters**.

### ⚡ **Decoupled Event System**
- **Instance-Based EventBus:** A thread-safe, high-performance message bus for system-wide communication, accessible via `Flux.Manager.EventBus`.
- **Attribute-Based Subscriptions:** Subscribe to any `FluxEvent` or `ReactiveProperty` change with a single attribute (`[FluxEventHandler]`, `[FluxPropertyChangeHandler]`).
- **Automatic Lifecycle Management:** The framework handles subscription and unsubscription automatically, preventing memory leaks.

### ✅ **Integrated Testing Framework**
- **Test in Isolation:** A built-in test runner and framework allows you to write fast, reliable unit and integration tests for your game logic.
- **Declarative Tests:** Write tests using `[FluxTest]` attributes, inheriting from a `FluxTestBase` class that provides a sandboxed, in-memory version of the framework.
- **CI/CD Ready:** Includes a command-line interface to easily integrate your test suite into any continuous integration pipeline (e.g., GitHub Actions).

### 🛠️ **Superior Developer Experience**
- **Rich Editor Tooling:** A full suite of editor windows, including a central **Control Panel**, a live **Reactive Properties Inspector**, and an **Event Bus Monitor**.
- **Project Health Check:** A powerful auditor that scans your entire project for broken or misspelled bindings.
- **Type-Safe Code Generation:** A built-in tool generates a static `FluxKeys` class for compile-time safety and auto-completion.
- **Intelligent Inspectors:** Custom editors provide a clean, feature-rich experience with `[FluxGroup]`, `[FluxButton]`, and `[FluxAction]`.
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
```**Key Concept:** The standard Unity methods (`Awake`, `Start`, `OnDestroy`) are sealed in the base class to prevent initialization order issues. **Always use the `OnFlux...` methods.**

### 2. Defining and Modifying State

You can declare state in two ways.

#### The Simple Pattern (Implicit)
Declare your state with a primitive type and the `[ReactiveProperty]` attribute. This is great for simplicity and quick setup.

```csharp
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerStats : FluxMonoBehaviour
{
    // The attribute declares the property and its initial value.
    // The private field will be kept in sync by the framework.
    [ReactiveProperty("player.health")]
    private float _health = 100f;

    [FluxButton("Take 10 Damage")]
    private void TakeDamage()
    {
        // To safely MODIFY the state, provide a function that transforms its current,
        // authoritative value. This prevents bugs from using a stale local cache.
        UpdateReactiveProperty("player.health", currentHealth => currentHealth - 10f);
    }
}
```

#### The Advanced Pattern (Explicit)
For properties that are frequently updated or require maximum type safety, declare the field as a `ReactiveProperty<T>` directly.

```csharp
public class PlayerStatsAdvanced : FluxMonoBehaviour
{
    // The field itself is the reactive property.
    [ReactiveProperty("player.ammo")]
    private ReactiveProperty<int> _ammo = new ReactiveProperty<int>(25);

    // Subscriptions and modifications are direct, faster, and type-safe.
    protected override void OnFluxAwake()
    {
        _ammo.Subscribe(OnAmmoChanged);
    }

    public void FireWeapon()
    {
        _ammo.Value--; // Direct, no "magic strings" needed.
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
using Flux; // Namespace for the generated FluxKeys class

public class PlayerHUD : FluxUIComponent
{
    // 1. Reference your UI element.
    // 2. Add the [FluxBinding] attribute.
    // 3. Use the generated FluxKeys class for a type-safe key.
    [Header("Bindings")]
    [FluxBinding(FluxKeys.PlayerHealth)] // No more magic strings!
    [SerializeField] private TextMeshProUGUI _healthText;

    // That's it! The base class handles the rest.
}
```

### 4. Reacting to State Changes and Events (Declarative)

Use attributes to turn methods into automatic listeners.

```csharp
using FluxFramework.Core;
using FluxFramework.Attributes;
using Flux; // Namespace for the generated FluxKeys class

public class AudioFeedback : FluxMonoBehaviour
{
    // This method is now automatically subscribed to the "player.health" property.
    // It will be called every time its value changes.
    [FluxPropertyChangeHandler(FluxKeys.PlayerHealth)]
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

## ✅ Writing Your First Test

Flux 3.0 introduces a powerful, built-in testing framework. Write tests for your game logic by inheriting from `FluxTestBase`, which provides a clean, in-memory version of the framework for each test.

```csharp
using FluxFramework.Testing;
using FluxFramework.Testing.Attributes;

public class MyGameLogicTests : FluxTestBase
{
    // The `Manager` property gives you access to the sandboxed framework instance.
    
    [FluxTest]
    public void AddToInventory_WhenItemIsValid_ShouldUpdateGoldProperty()
    {
        // --- ARRANGE ---
        var inventory = new InventoryManager(Manager); // Your class might need the manager
        const string goldKey = "player.gold";
        Manager.Properties.GetOrCreateProperty(goldKey, 100);

        // --- ACT ---
        inventory.AddItem(new Item { Value = 50 });

        // --- ASSERT ---
        var finalGold = Manager.Properties.GetProperty<int>(goldKey).Value;
        Assert(finalGold == 150, "Gold should have increased by 50.");
    }
}
```
Run your tests from the **Flux > Testing > Test Runner...** window.

## 🛠️ Best Practices

*   **Logic Creates State:** Use `FluxMonoBehaviour` or `FluxDataContainer` to define your game's state with `[ReactiveProperty]`.
*   **Views Display State:** Use `FluxUIComponent` and `[FluxBinding]` to display data. UI components should contain minimal or no logic.
*   **Communicate via Events:**
    *   From a `FluxMonoBehaviour`, always prefer the built-in helper: `PublishEvent(new MyEvent());`
    *   From any other class, use the full path via the manager: `Flux.Manager.EventBus.Publish(new MyEvent());`
*   **Prefer Type-Safe Keys:** Run the **Keys Generator** (`Flux/Tools/Generate Static Keys...`) and use the `FluxKeys` class instead of raw strings to prevent errors.
*   **Validate Your Project:** Regularly use the **Health Check** tool (`Flux/Tools/Run Health Check...`) to find and fix broken bindings.

---

## 📚 Documentation

For more detailed information, please refer to the specific documentation files:

-   **[TUTORIAL.md](./TUTORIAL.md):** A step-by-step guide to building a complete game with FluxFramework.
-   **[REFERENCE_GUIDE.md](./REFERENCE_GUIDE.md):** A quick reference for all editor menus and C# attributes.
-   **[VR_README.md](./VR_README.md):** Documentation for the Virtual Reality extension.
-   **[VISUAL_SCRIPTING_README.md](./VISUAL_SCRIPTING_README.md):** Documentation for the Visual Scripting system.
-   **[TESTING_GUIDE.md](./TESTING_GUIDE.md):** A guide to using the integrated testing framework and setting up CI/CD.

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.