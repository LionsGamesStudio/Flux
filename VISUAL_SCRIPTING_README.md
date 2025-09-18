# 🎨 FluxFramework - Visual Scripting

**A powerful, attribute-based visual scripting system for the FluxFramework ecosystem.**

## 🚀 Overview

The FluxFramework visual scripting system enables you to create complex game logic visually using a node-based graph editor. It is deeply integrated with the framework's core reactive systems, allowing for seamless communication between your C# code and your visual graphs.

The architecture is built for robustness and extensibility. It features an **asynchronous, coroutine-based execution engine** that handles complex time-based and concurrent logic without blocking your game. The data flow model uses a **"Just-In-Time" execution** strategy, ensuring that data is calculated only when needed, providing optimal performance.

## ✨ Core Features

### 🔗 **Deep Framework Integration**
- **Reactive Property Nodes:** `Get` and `Set` any reactive property by its key, creating a direct bridge to your game's state.
- **Flux Event Nodes:** Dedicated nodes to listen for (`Listen for Flux Event`) and publish (`Publish Flux Event`) strongly-typed framework events, enabling a fully event-driven architecture.
- **Component & GameObject Lifecycle:** Full control over the scene with nodes to `Instantiate`, `Destroy`, `GetComponent`, and `AddComponent`.
- **Context-Aware Execution:** Nodes have access to the `GameObject` running the graph, allowing for powerful, scene-aware interactions.

### ⚡ **Asynchronous & Intelligent Flow Control**
- **Non-Blocking Logic:** Nodes that take time (`Delay`, `Timer`, `ForEach`) are fully asynchronous and managed safely by the execution engine, never freezing your game.
- **Concurrent Execution:** Execution outputs with multiple connections (`PortCapacity.Multi`) will correctly split the flow, running all branches.
- **Advanced Flow Control:** A rich library including `Branch` (If/Else), `Sequence` (Then 0, Then 1...), `Relay` (to merge flows), and `Sub-Graph` execution.

### 🛠️ **Attribute-Based Node Creation**
- **Pure C# Logic:** Create new nodes by writing simple, clean C# classes that implement the `INode` interface. No boilerplate or complex editor code required.
- **Declarative Ports:** Define input and output ports directly on your class fields using the `[Port]` attribute.
- **Automatic Discovery:** The visual editor automatically finds any class marked with the `[FluxNode]` attribute and adds it to the node creation menu.

### 🎨 **Professional Editor Experience**
- **Intuitive Graph Editor:** A fluid, node-based editor built on Unity's modern GraphView API.
- **Real-Time Visual Debugging:** See your logic execute in real-time. Nodes in execution gain a glowing overlay, and connections flash as execution tokens pass through them.
- **Smart Node Creation:** A searchable window (`Right-Click` or drag from a port) that discovers all available nodes.
- **Rich Node Inspector:** A dedicated inspector displays properties for any selected node, fully supporting custom layouts and inline fields for unconnected data ports.

## 🎯 Installation and Usage

The Visual Scripting module is an integral part of the FluxFramework.

### 1. Create a Visual Graph
- In the Project window, right-click → `Create` → `Flux` → `Visual Scripting` → `New Graph`.
- Name the new asset (e.g., `PlayerLogicGraph`).

### 2. Use the Visual Script Component (Runner)
- On a GameObject in your scene, add the **`FluxVisualScriptComponent`**.
- Drag your graph asset into the `Graph` slot.
- The `Execute On Start` option is enabled by default.

### 3. Open the Editor
- With the graph asset selected, click the **"Open in Visual Editor"** button in the inspector.
- Alternatively, use the Unity Menu: `Flux` → `Visual Scripting` → `Visual Scripting Editor`.

## 🏗️ Creating Custom Nodes

Creating your own nodes is the core strength of this system. It's simple, clean, and attribute-driven.

```csharp
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

// 1. Add the [FluxNode] attribute to define its name and category.
[System.Serializable]
[FluxNode("Move Towards", Category = "Transform", Description = "Moves a Transform towards a target position.")]
public class MoveTowardsNode : IExecutableNode // 2. Implement IExecutableNode for action nodes.
{
    // --- Execution Ports ---
    [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] 
    public ExecutionPin In;
    
    [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] 
    public ExecutionPin Out;

    // --- Data Ports ---
    // These will appear as input sockets on the node.
    [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform Target;
    [Port(FluxPortDirection.Input, "Destination", PortCapacity.Single)] public Vector3 Destination;
    [Port(FluxPortDirection.Input, "Speed", PortCapacity.Single)] public float Speed;

    // 3. Implement the logic.
    public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
    {
        if (Target != null)
        {
            float step = Speed * Time.deltaTime;
            Target.position = Vector3.MoveTowards(Target.position, Destination, step);
        }
    }
}
```

## 🎮 Usage Example: A Reactive Health System

This example shows how a graph can listen for a `TakeDamageEvent`, update a reactive property, and publish another event when the player's health is depleted.

1.  **`On Start` Node:**
    *   Connect its `Out` port to a `Set Flux Property` node to initialize `"player.health"` to `100`.

2.  **`Listen for Flux Event` Node:**
    *   **Event Type Name:** `"MyGame.Events.TakeDamageEvent"` (assuming this event has a `float DamageAmount` property).
    *   This node will execute its `Out` port every time a `TakeDamageEvent` is published.

3.  **Get Current Health:**
    *   Connect the `Out` of the listener to a **`Get Flux Property`** node with the key `"player.health"`.

4.  **Calculate New Health:**
    *   Use a **`Subtract`** node. Connect the `Value` from `Get Flux Property` to input `A`. Connect the `DamageAmount` output from the `Listen for Flux Event` node to input `B`.

5.  **Update Health:**
    *   Use a **`Set Flux Property`** node with the key `"player.health"`. Connect the result of the `Subtract` node to its `Value` input.

6.  **Check for Death (`Branch`):**
    *   After setting the new health, use a **`Compare (Float)`** node (`<= 0`).
    *   Connect the result to a **`Branch`** node.
    *   If `True`, connect to a **`Publish Flux Event`** node with the event type `"MyGame.Events.PlayerDiedEvent"`.

**Result:** This graph creates a complete, event-driven health system without writing a single `Update()` loop, showcasing the power of integrating reactive properties and events visually.