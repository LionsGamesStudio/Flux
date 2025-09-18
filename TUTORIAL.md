# 🎮 FluxFramework Tutorial - Build Your First Reactive Game

**Learn FluxFramework by creating a complete platformer with a reactive inventory and a fully automated UI!**

In this tutorial, we'll demonstrate the power of FluxFramework's reactive architecture by building a simple platformer game from scratch. We will establish a clean separation of concerns using a `ScriptableObject` for our **Data**, `MonoBehaviour`s for our **Logic**, and dedicated `FluxUIComponent`s for our **View**.

---

## 📋 Table of Contents

1.  [Project Setup](#1-project-setup)
2.  [Creating the Player Data Container (The "Model")](#2-creating-the-player-data-container-the-model)
3.  [Generating Type-Safe Keys](#3-generating-type-safe-keys)
4.  [Creating the Player Controller (The "Logic")](#4-creating-the-player-controller-the-logic)
5.  [Building the Reactive UI (The "View")](#5-building-the-reactive-ui-the-view)
6.  [Adding Collectible Items (Interaction)](#6-adding-collectible-items-interaction)
7.  [Connecting Systems with Events](#7-connecting-systems-with-events)

---

## 1. Project Setup

### Step 1.1: Install FluxFramework
1.  **Open Unity** (2021.3 or newer).
2.  **Create a new 2D project**.
3.  **Install FluxFramework** via the Package Manager.

### Step 1.2: Scene Setup
1.  Create a new scene named "PlatformerDemo".
2.  Create "Ground" and "Platform" GameObjects using 2D Square sprites with `BoxCollider2D`.
3.  Create a "Player" GameObject with a `SpriteRenderer`, `Rigidbody2D`, `BoxCollider2D`, and the **"Player" tag**.
4.  Add a child `GameObject` to the Player named "GroundCheck" and position it slightly below the Player's feet.
5.  Create a "Ground" layer in Unity and assign all ground/platform objects to this layer.

**✅ Checkpoint 1**: You have a simple scene ready for gameplay.

---

## 2. Creating the Player Data Container (The "Model")

Instead of scattering our game's state across various `MonoBehaviour`s, we'll centralize it in a `FluxDataContainer`. This `ScriptableObject` will be our single source of truth for all player-related data.

### Step 2.1: Create the `PlayerData` Script
1.  **Use the Template Generator:** In your project (e.g., in `Assets/Scripts/Data/`), right-click → `Create` → `Flux` → `Framework` → `FluxDataContainer`.
2.  Name it `PlayerData`.
3.  Open the new file and add the following properties. This will be the complete definition of our player's state.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Flux/Tutorial/Player Data")]
public class PlayerData : FluxDataContainer
{
    [Header("Player Stats")]
    // The framework will create a ReactiveProperty named "player.health".
    [ReactiveProperty("player.health", Persistent = false)] // Health resets when the game restarts.
    [FluxRange(0, 100)]
    public float Health = 100f;

    [ReactiveProperty("player.isGrounded", Persistent = false)]
    public bool IsGrounded;

    [ReactiveProperty("player.position", Persistent = false)]
    public Vector2 Position;
    
    [Header("Inventory")]
    [ReactiveProperty("player.gold", Persistent = true)] // Gold is saved between sessions.
    [FluxRange(0, 99999)]
    public int Gold;
}
```

### Step 2.2: Create the Data Asset
1.  In your Unity project (e.g., in `Assets/Data/`), right-click → `Create` → `Flux` → `Tutorial` → `Player Data`.
2.  Name the created asset `MyPlayerData`. This file now holds your game's state definitions and initial values.

**✅ Checkpoint 2**: You have a central `ScriptableObject` that defines and holds all the data for your player.

---

## 3. Generating Type-Safe Keys

To avoid typos when referencing our properties (like `"player.health"`), we'll use Flux's code generator to create a static `FluxKeys` class.

### Step 3.1: Generate the Keys
1.  Open the generator window: **Flux → Tools → Generate Static Keys...**
2.  Click **"Scan Project and Generate Keys"**.
3.  A new file, `Assets/Flux/Generated/FluxKeys.cs`, will be created, containing static constants for all the keys we defined in `PlayerData`.

**✅ Checkpoint 3**: You can now access your property keys safely with `FluxKeys.PlayerHealth`, `FluxKeys.PlayerGold`, etc.

---

## 4. Creating the Player Controller (The "Logic")

This script's only job is to handle player input and physics. It doesn't own any data; it simply **sends update requests** to the central state manager.

### Step 4.1: Create the `PlayerController` Script
1.  Create a new `FluxMonoBehaviour` script named `PlayerController`.
2.  Attach it to your "Player" GameObject and configure the inspector fields.

### Step 4.2: Implement Player Logic
```csharp
using UnityEngine;
using FluxFramework.Core;
using Flux; // <-- Import the generated keys namespace

public class PlayerController : FluxMonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Physics Dependencies")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    
    private Rigidbody2D _rb;

    protected override void OnFluxAwake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // Reset non-persistent stats at the start of the game
        UpdateReactiveProperty(FluxKeys.PlayerHealth, 100f);
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        _rb.velocity = new Vector2(horizontal * moveSpeed, _rb.velocity.y);

        // To READ a value, get it from the central manager.
        bool isGrounded = GetReactivePropertyValue<bool>(FluxKeys.PlayerIsGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            PublishEvent(new PlayerJumpedEvent());
        }
    }

    private void FixedUpdate()
    {
        // To WRITE state, we must use the helper method. This ensures
        // the central ReactiveProperty is updated, and all listeners are notified.
        bool groundedState = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        UpdateReactiveProperty(FluxKeys.PlayerIsGrounded, groundedState);
        
        UpdateReactiveProperty(FluxKeys.PlayerPosition, (Vector2)_rb.position);
    }
}

// Create this event class in a new file, e.g., PlayerEvents.cs
public class PlayerJumpedEvent : FluxEventBase { }
```

**✅ Checkpoint 4**: Your player moves, and the framework's state is being updated correctly.

---

## 5. Building the Reactive UI (The "View")

Now for the magic. The UI will have its own script, but it will contain **zero lines of binding code**.

### Step 5.1: Create the UI
Create a `Canvas` and add `TextMeshPro - Text` elements for: `HealthText`, `PositionText`, and `GoldText`.

### Step 5.2: Create the `PlayerHUD` Script
1.  Use the template generator to create a **`FluxUIComponent`** named `PlayerHUD`.
2.  Attach it to a "HUD" GameObject under your Canvas.
3.  Add this code. It's purely declarative!

```csharp
using UnityEngine;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Core;
using Flux; // <-- Import the generated keys namespace

public class PlayerHUD : FluxUIComponent
{
    [Header("Component References & Bindings")]
    
    // The [FluxBinding] attribute tells the framework to bind this UI component
    // to the specified property key. The base class handles all the work.
    
    [FluxBinding(FluxKeys.PlayerHealth)]
    [SerializeField] private TextMeshProUGUI _healthText;

    [FluxBinding(FluxKeys.PlayerPosition)]
    [SerializeField] private TextMeshProUGUI _positionText;
    
    [FluxBinding(FluxKeys.PlayerGold, ConverterType = typeof(GoldToTextConverter))]
    [SerializeField] private TextMeshProUGUI _goldText;
}

/// <summary>
/// A custom Value Converter that transforms an integer (gold) into a formatted string.
/// This can be in its own file.
/// </summary>
public class GoldToTextConverter : IValueConverter<int, string>
{
    public string Convert(int value) => $"Gold: {value}";
    public int ConvertBack(string value) => 0; // Not needed for OneWay binding

    // Implement the non-generic members explicitly
    object IValueConverter.Convert(object value) => Convert((int)value);
    object IValueConverter.ConvertBack(object value) => ConvertBack((string)value);
}
```

**✅ Checkpoint 5**: Assign the Text components to the `PlayerHUD` inspector slots. Press Play. The UI updates automatically as the player moves and jumps!

---

## 6. Adding Collectible Items (Interaction)

Now we need a system to modify our inventory. We'll create a dedicated `InventoryManager` for this business logic.

### Step 6.1: Create the `InventoryManager`
1.  Create an empty GameObject named "InventoryManager".
2.  Create a `FluxMonoBehaviour` named `InventoryManager` and attach it.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using Flux;

// Custom event for when gold changes, to carry the new total.
public class GoldChangedEventArgs : FluxEventBase
{
    public int NewGoldTotal { get; }
    public GoldChangedEventArgs(int newTotal) { NewGoldTotal = newTotal; }
}

public class InventoryManager : FluxMonoBehaviour
{
    [FluxAction("Add 10 Gold")] // Makes this method a button in the Inspector!
    public void AddGold(int amount = 10)
    {
        if (amount <= 0) return;

        // --- THIS IS THE CORRECT WAY TO UPDATE A REACTIVE PROPERTY ---
        // 1. Use the UpdateReactiveProperty helper with a function.
        // This reads the current authoritative value, modifies it, and writes it back
        // in one safe, atomic operation that respects validation.
        UpdateReactiveProperty<int>(FluxKeys.PlayerGold, currentGold => currentGold + amount);
        
        // 2. Publish an event to notify other systems of the change.
        int newTotal = GetReactivePropertyValue<int>(FluxKeys.PlayerGold);
        PublishEvent(new GoldChangedEventArgs(newTotal));
    }
}
```

### Step 6.2: Create a Collectible
Create a `FluxMonoBehaviour` named `Collectible.cs` and create a prefab from it.
```csharp
using UnityEngine;
using FluxFramework.Core;

public class Collectible : FluxMonoBehaviour
{
    [SerializeField] private int goldValue = 10;
    private static InventoryManager _inventoryManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_inventoryManager == null)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
        }

        if (_inventoryManager != null)
        {
            _inventoryManager.AddGold(goldValue);
            Destroy(gameObject);
        }
    }
}
```

**✅ Checkpoint 6**: Place some "GoldCoin" prefabs in your scene. When the player touches one, the gold count in your UI updates instantly!

---

## 7. Connecting Systems with Events

Finally, let's create a completely decoupled `AudioManager` that listens for the `PlayerJumpedEvent`.

### Step 7.1: Create the Audio Manager
Create a `FluxMonoBehaviour` named `AudioManager.cs` and place it in your scene.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class AudioManager : FluxMonoBehaviour
{
    // This attribute is all you need. The framework will automatically
    // subscribe this method to any PlayerJumpedEvent published on the EventBus.
    [FluxEventHandler]
    private void OnPlayerJumped(PlayerJumpedEvent evt)
    {
        Debug.Log("AudioManager heard PlayerJumpedEvent! Playing jump sound.");
    }
    
    // We can even listen for the gold change event from the InventoryManager.
    [FluxEventHandler]
    private void OnGoldChanged(GoldChangedEventArgs evt)
    {
        Debug.Log($"AudioManager heard gold changed! New total: {evt.NewGoldTotal}. Playing coin sound.");
    }
}
```

**Key Concept:** The `PlayerController` and `InventoryManager` publish events using the `PublishEvent()` helper method. This is a convenient shortcut provided by `FluxMonoBehaviour`. Under the hood, it simply calls **`Flux.Manager.EventBus.Publish()`**. This is how all systems communicate without needing direct references to each other.

**🎉 Congratulations!** You have now built a small, complete game loop using FluxFramework, demonstrating a clean separation between Data (`PlayerData`), Logic (`PlayerController`, `InventoryManager`), and View (`PlayerHUD`).