# 🎨 FluxFramework - Visual Scripting

**A powerful, fully-integrated visual scripting system for the FluxFramework ecosystem.**

## 🚀 Overview

The FluxFramework visual scripting system enables you to create complex game logic visually using a node-based graph editor. It is deeply integrated with the framework's core reactive systems, allowing for seamless communication between your C# code and your visual graphs.

The architecture is built for robustness, featuring a **context-aware execution engine** that enables powerful, scene-aware nodes and **automatic lifecycle management** to prevent common issues like memory leaks.

## ✨ Core Features

### 🔗 **Deep Framework Integration**
- **Reactive Property Nodes:** A single, powerful `ReactivePropertyNode` to Get, Set, Subscribe, Unsubscribe, and check for the existence of any reactive property.
- **Flux Event Nodes:** Dedicated nodes to listen for (`FluxEventListenerNode`) and publish (`FluxEventPublishNode`) strongly-typed framework events.
- **UI Binding Control:** Trigger UI components to register or unregister their bindings directly from a graph.
- **Context-Aware Execution:** Nodes have access to the `GameObject` that is running the graph, allowing for powerful interactions like `GetComponent`, `StartCoroutine`, and safe, instance-based subscriptions.

### 🎯 **Comprehensive Node Library**

#### **Framework**
-   `Reactive Property`: The all-in-one node for state management.
-   `Publish Flux Event` / `Listen to Flux Event`: For communication with the `EventBus`.
-   `UI Binding`: Manually control the lifecycle of `FluxUIComponent` bindings.
-   `Data Container`: Serialize, deserialize, and validate `FluxDataContainer` assets.
-   `Component`: A safe way to `GetComponent` or `AddComponent` on GameObjects.

#### **Flow Control**
-   `Start`: The primary entry point for a graph.
-   `Branch` (If/Else), `Switch`: For conditional logic.
-   `For Loop`, `Timer`, `Delay`: Asynchronous nodes that don't block execution and use coroutines managed by the framework.
-   `Execute Function`: Allows you to create reusable sub-routines within a single graph.

#### **Data, Logic & Math**
-   `Constant`: Provides values of any basic type.
-   `Comparison`: Compares two inputs.
-   `Math Operation`: A full suite of mathematical functions.
-   `Random`: Generates random numbers, vectors, or booleans.
-   `Debug Log`: Prints messages to the console with context.

### 🛠️ **Professional Editor Experience**
- **Intuitive Graph Editor:** A fluid, node-based editor built on Unity's GraphView API.
- **Smart Node Creation:** A searchable window (`Ctrl+Space`) that automatically discovers all available nodes.
- **Live Validation:** The graph provides real-time visual feedback, highlighting nodes with unconnected required ports.
- **Rich Node Inspector:** A dedicated inspector panel displays detailed properties and actions for any selected node, fully supporting custom editors.

## 🎯 Installation and Usage

The Visual Scripting module is an integral part of the FluxFramework.

### 1. Create a Visual Graph
- In the Project window, right-click → `Create` → `Flux` → `Visual Scripting` → `Graph`.
- Name the new asset (e.g., `PlayerLogicGraph`).

### 2. Use the Visual Script Component (Runner)
- On a GameObject in your scene, add the **`FluxVisualScriptComponent`**.
- Drag your graph asset into the `Graph` slot.
- Configure when it should run (e.g., `Execute On Start`).

### 3. Open the Editor
- With the graph asset selected, click the **"Open in Visual Editor"** button in the inspector.
- Alternatively, use the Unity Menu: `Flux` → `Visual Scripting Editor`.

## 🏗️ Creating Custom Nodes

Creating your own nodes is straightforward. Inherit from `FluxNodeBase` and implement two methods.

```csharp
[CreateAssetMenu(menuName = "Flux/Visual Scripting/Custom/My Custom Node")]
public class MyCustomNode : FluxNodeBase
{
    // 1. Define your ports.
    protected override void InitializePorts()
    {
        AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
        AddInputPort("speed", "Speed", FluxPortType.Data, "float", false, 5.0f);
        AddOutputPort("onComplete", "▶ Out", FluxPortType.Execution, "void", false);
    }

    // 2. Implement your logic.
    protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
    {
        // Get the GameObject running this graph.
        var contextObject = executor.Runner.GetContextObject();
        float speed = GetInputValue<float>(inputs, "speed");

        Debug.Log($"Executing on '{contextObject.name}' with speed {speed}!", contextObject);

        // Continue the execution flow.
        SetOutputValue(outputs, "onComplete", null);
    }
}
```

## 🎮 Usage Example: A Reactive Health System

This example shows how a graph can listen for a property change and publish an event.

1.  **`ReactivePropertyNode` (Subscribe):**
    *   **Action:** `Subscribe`
    *   **Property Key:** `"player.health"`
    *   **Context:** Connect a "Get Self" node to this port.
    *   The `onChanged` execution port will fire every time the player's health changes.

2.  **`ComparisonNode`:**
    *   Connect the `value` output of the first node to the `A` input here.
    *   **Operation:** `LessEqual`
    *   **B:** A `ConstantNode` with an `Int` value of `0`.

3.  **`BranchNode`:**
    *   Connect the `onChanged` from the first node to the `execute` port of the Branch.
    *   Connect the `result` of the ComparisonNode to the `condition` port.

4.  **`Publish Flux EventNode`:**
    *   Connect the `▶ True` output of the BranchNode to this node's `execute` port.
    *   **Event Type Name:** `"MyGame.Events.PlayerDiedEvent"`

**Result:** This graph automatically listens for health changes and publishes a `PlayerDiedEvent` only when health drops to 0 or below, without any `Update()` loop.

## 🚀 Performance and Architecture

-   **Asynchronous by Design:** Nodes that take time (`Delay`, `Timer`, `ForLoop`) use non-blocking coroutines, managed safely by the `IGraphRunner`.
-   **Context-Aware:** Nodes have access to the `FluxGraphExecutor` and `IGraphRunner`, allowing for safe, instance-based state management and callbacks (e.g., event subscriptions).
-   **Optimized Execution:** The `FluxGraphExecutor` uses a topological sort for pure data nodes and caches output values to avoid redundant calculations within a single execution.
-   **Validation:** The graph and its nodes provide validation feedback in the editor, preventing common errors before the game is even run.

This robust architecture makes the FluxFramework visual scripting system a powerful, reliable, and scalable tool for any project.