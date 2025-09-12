# 📖 FluxFramework - Editor & Attributes Reference

This document provides a quick reference guide to the editor menus and the most important C# attributes available in the FluxFramework.

## 📋 Table of Contents

1.  [Editor Menus](#1-editor-menus)
2.  [Core Framework Attributes](#2-core-framework-attributes)
3.  [UI Binding Attributes](#3-ui-binding-attributes)
4.  [Editor Enhancement Attributes](#4-editor-enhancement-attributes)

---

## 1. Editor Menus

The FluxFramework integrates directly into the Unity editor through the main `Flux` menu and context menus.

### Main Menu: `Flux/`

| Menu Path | Description |
| :--- | :--- |
| **Control Panel...** | Opens the main **Flux Control Panel**, which is the central hub for accessing all other tools, viewing live stats, and managing the framework. |
| --- | --- |
| **Tools/** | Contains tools that scan or modify your project assets. |
| `› Run Health Check...` | Opens the **Health Check** window to scan the project for broken bindings and other common configuration errors. |
| `› Generate Static Keys...`| Opens the **Keys Generator** window, which scans for all `[ReactiveProperty]` attributes and creates the `FluxKeys.cs` file for type-safe access. |
| `› Refresh Component Registry` | Manually forces the framework to re-scan all assemblies for `[FluxComponent]` attributes. Useful if automatic discovery seems out of sync. |
| `› Refresh Event Types` | Manually forces a re-scan of all `IFluxEvent` types for the Visual Scripting editor. |
| **Debug/** | Contains tools that are primarily used at runtime to monitor the application's state. |
| `› Reactive Properties Inspector...` | Opens a window that displays all registered `ReactiveProperty` instances at runtime, allowing you to **view and modify their values live**. |
| `› Event Bus Monitor...` | Opens a window that displays a real-time log of every event that passes through the global `EventBus`. |
| **Configuration/** | Provides quick access to the framework's core configuration assets. |
| `› Framework Settings...` | Selects the `FluxFrameworkSettings.asset` in the Project window. |
| `› UI Theme...` | Selects the first `FluxUITheme.asset` found in the project. |
| `› Property Definitions...` | Opens the **Property Key Viewer** window, which lists all keys defined in `FluxPropertyDefinitions` assets. |
| `› Event Definitions...` | Selects the first `FluxEventDefinitions.asset` found in the project. |
| --- | --- |
| **Documentation** | Opens the official documentation page in your web browser. |

### Context Menu: `Assets/Create/Flux/`

This menu allows you to quickly create new, pre-configured C# scripts based on the framework's core classes.

| Menu Path | Description |
| :--- | :--- |
| **Framework/** | For creating core logic and data classes. |
| `› FluxMonoBehaviour` | Creates a new component that inherits from `FluxMonoBehaviour`, with the safe lifecycle methods already stubbed out. |
| `› FluxDataContainer` | Creates a new `ScriptableObject` data container that inherits from `FluxDataContainer`. |
| `› FluxSettings` | Creates a new `ScriptableObject` for game settings that inherits from `FluxSettings` (with auto-saving features). |
| **UI/** | For creating new UI components. |
| `› FluxUIComponent` | Creates a new UI component that inherits from `FluxUIComponent`, ready for data binding. |
| **Event/** | For creating new event types. |
| `› Flux Event` | Creates a new class that inherits from `FluxEventBase`, ready to carry data. |
| **Visual Scripting/** | For extending the visual scripting system. |
| `› New Node` | Creates a new class that inherits from `FluxNodeBase`, ready to implement custom logic. |

### Context Menu: Inspector `Add Component`

All built-in `FluxUIComponent`s can be found in the Inspector's "Add Component" button under the **Flux/UI/** submenu.

---

## 2. Core Framework Attributes

These attributes are used to integrate your C# classes with the framework's core systems.

### `[ReactiveProperty(string key, bool Persistent = false)]`
-   **Target:** Field (`int`, `float`, `string`, `ReactiveProperty<T>`, etc.)
-   **Purpose:** Declares a field as a reactive property and registers it with the global `FluxManager`. This is the foundation of the state management system.
-   **Parameters:**
    -   `key`: The unique string identifier for this property (e.g., `"player.health"`).
    -   `Persistent`: If `true`, the property's value will be saved and reloaded between game sessions. Defaults to `false`.

### `[FluxEventHandler]`
-   **Target:** Method
-   **Purpose:** Automatically subscribes the decorated method to the global `EventBus`. The event type is inferred from the method's single parameter. The framework automatically handles unsubscription.
-   **Example:** `[FluxEventHandler] private void OnPlayerDied(PlayerDiedEvent evt) { ... }`

### `[FluxPropertyChangeHandler(string propertyKey)]`
-   **Target:** Method
-   **Purpose:** Automatically subscribes the decorated method to a specific `ReactiveProperty`. The method will be called every time the property's value changes.
-   **Parameters:**
    -   `propertyKey`: The key of the property to listen to (e.g., `FluxKeys.PlayerHealth`).
-   **Signatures:** The method can have 0, 1 (new value), or 2 (old value, new value) parameters.

### `[FluxComponent]`
-   **Target:** Class (that inherits from `MonoBehaviour`)
-   **Purpose:** Marks a `MonoBehaviour` as a component that should be recognized and managed by the framework. This is the base attribute for classes like `FluxMonoBehaviour`.

---

## 3. UI Binding Attributes

These attributes are used to connect your UI to the framework's state.

### `[FluxBinding(string propertyKey, ...)]`
-   **Target:** Field (of a type that inherits from `UnityEngine.Component`, e.g., `Slider`, `TextMeshProUGUI`)
-   **Purpose:** The core of the UI system. It automatically creates a data binding between the UI component and a `ReactiveProperty`.
-   **Parameters:**
    -   `propertyKey`: The key of the property to bind to (e.g., `FluxKeys.PlayerHealth`).
    -   `Mode` (optional): The `BindingMode` (`OneWay`, `TwoWay`). Defaults to `OneWay`.
    -   `ConverterType` (optional): The `Type` of a class that implements `IValueConverter` to handle type mismatches (e.g., `typeof(IntToStringConverter)`).

---

## 4. Editor Enhancement Attributes

These attributes are used to improve the inspector experience for your custom components.

### `[FluxGroup(string groupName, int order = 0)]`
-   **Target:** Field
-   **Purpose:** Organizes serialized fields in the inspector into collapsible foldout groups.
-   **Parameters:**
    -   `groupName`: The name of the foldout group.
    -   `order`: A number used to control the drawing order of the groups (lower numbers appear first).

### `[FluxButton(string buttonText = "", ...)]`
-   **Target:** Method (with no parameters)
-   **Purpose:** Renders a method with no parameters as a clickable button in the inspector.
-   **Parameters:**
    -   `buttonText`: The text to display on the button. If empty, the method name is used.
    -   `EnabledInPlayMode`/`EnabledInEditMode`: Controls when the button is clickable.

### `[FluxAction(string displayName = "", ...)]`
-   **Target:** Method (with parameters)
--   **Purpose:** Renders a method with parameters as a "mini-form" in the inspector, with fields for each parameter and a button to invoke the method.
-   **Parameters:**
    -   `displayName`: The title for the action block.
    -   `buttonText`: The text for the invoke button.