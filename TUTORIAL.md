# 🎮 FluxFramework Tutorial - Build Your First Reactive Game

**Learn FluxFramework by creating a complete platformer with a reactive inventory and a fully automated UI!**

In this tutorial, we'll demonstrate the power of FluxFramework's reactive architecture by building a simple platformer game from scratch, showcasing a clean separation between logic, data, and UI.

---

## 📋 Table of Contents

1.  [Project Setup](#1-project-setup)
2.  [Creating the Player Controller (Logic)](#2-creating-the-player-controller-logic)
3.  [Creating the Inventory (Data)](#3-creating-the-inventory-data)
4.  [Building the Reactive UI (View)](#4-building-the-reactive-ui-view)
5.  [Adding Collectible Items (Interaction)](#5-adding-collectible-items-interaction)

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

## 2. Creating the Player Controller (Logic)

This script will manage the player's state (position, movement). It only handles logic and knows nothing about the UI.

### Step 2.1: Create the PlayerController Script
**🛠️ Use the Template Generator:**
1.  In `Assets/Scripts/Player/`, right-click → `Create` → `Flux` → `Framework` → `FluxMonoBehaviour`.
2.  Name the script `PlayerController`.

### Step 2.2: Implement Player Logic
Open `PlayerController.cs` and add this code:

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerController : FluxMonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    // --- Reactive Properties ---
    // The framework's ComponentRegistry will find these attributes and create global
    // ReactiveProperties linked to these private fields.
    [ReactiveProperty("player.isGrounded")]
    private bool _isGrounded;

    [ReactiveProperty("player.position")]
    private Vector2 _position;

    private Rigidbody2D _rb;

    protected override void Awake()
    {
        base.Awake(); // Call base for framework integration
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // For a real game, use Unity's new Input System for better handling.
        float horizontal = Input.GetAxis("Horizontal");
        _rb.velocity = new Vector2(horizontal * moveSpeed, _rb.velocity.y);

        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);
            PublishEvent(new PlayerJumpedEvent());
        }
    }

    private void FixedUpdate()
    {
        // To update a reactive property, simply assign a new value to the field.
        // The framework handles the synchronization automatically.
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        _position = _rb.position;
    }
}

// Create this event in a new file, e.g., PlayerEvents.cs
public class PlayerJumpedEvent : FluxEventBase { }
```

**✅ Checkpoint 2**: Attach `PlayerController` to your Player, configure the inspector fields. You can now move and jump.

---

## 3. Creating the Inventory (Data)

We'll use a `FluxDataContainer` to manage inventory. This `ScriptableObject` holds data that persists and can be accessed globally.

### Step 3.1: Create the Inventory Data Script
1.  Use the template generator: `Create` → `Flux` → `Framework` → `FluxDataContainer`.
2.  Name it `PlayerInventory`.
3.  Add this code to `PlayerInventory.cs`:

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Flux/Tutorial/Player Inventory")]
public class PlayerInventory : FluxDataContainer
{
    [Header("Currency")]
    [ReactiveProperty("inventory.gold")]
    [FluxRange(0, 99999)] // Provides validation in the editor and at runtime!
    public int Gold;
}
```

### Step 3.2: Create the Inventory Asset and Manager
1.  **Create the Asset:** In your project, right-click → `Create` → `Flux` → `Tutorial` → `Player Inventory`. Name it `MyPlayerInventory`.
2.  **Create the Manager:** In your scene, create an empty GameObject named "InventoryManager". Create a new `FluxMonoBehaviour` script named `InventoryManager` and attach it.

### Step 3.3: Implement the `InventoryManager`
This manager contains the **business logic** for the inventory.

```csharp
using UnityEngine;
using FluxFramework.Core;

// Custom event for when gold changes.
public class GoldChangedEventArgs : FluxEventBase
{
    public int NewGoldTotal { get; }
    public GoldChangedEventArgs(int newTotal) { NewGoldTotal = newTotal; }
}

public class InventoryManager : FluxMonoBehaviour
{
    [Header("Data Asset")]
    [SerializeField] private PlayerInventory inventory;

    /// <summary>
    /// The business logic for adding gold.
    /// It modifies the data and announces the change with an event.
    /// </summary>
    public void AddGold(int amount)
    {
        if (inventory == null || amount <= 0) return;
        inventory.Gold += amount;
        PublishEvent(new GoldChangedEventArgs(inventory.Gold));
    }
}
```

**✅ Checkpoint 3**: You have a `PlayerInventory` asset holding your data and an `InventoryManager` in the scene to manage it. Assign `MyPlayerInventory` to the manager's inspector slot.

---

## 4. Building the Reactive UI (View)

Now, the magic of FluxFramework. We'll create a UI that updates automatically with **zero lines of binding code**.

### Step 4.1: Create the UI
1.  In your `Canvas`, create `TextMeshPro - Text` elements for:
    *   `GroundedStatusText`
    *   `PositionText`
    *   `GoldText`

### Step 4.2: Create the UI Script
1.  Use the template generator to create a **`FluxUIComponent`** named `PlayerHUD`.
2.  Attach it to a "HUD" GameObject under your Canvas.
3.  Add this code. **Notice how simple it is!**

```csharp
using UnityEngine;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

public class PlayerHUD : FluxUIComponent
{
    [Header("Component References & Bindings")]
    
    [Tooltip("Assign the Text for grounded status. The attribute binds it.")]
    [FluxBinding("player.isGrounded", ConverterType = typeof(BoolToGroundedTextConverter))]
    [SerializeField] private TextMeshProUGUI _groundedStatusText;

    [Tooltip("Assign the Text for the player's position.")]
    [FluxBinding("player.position", Mode = BindingMode.OneWay)]
    [SerializeField] private TextMeshProUGUI _positionText;
    
    [Tooltip("Assign the Text for the gold amount.")]
    [FluxBinding("inventory.gold")]
    [SerializeField] private TextMeshProUGUI _goldText;

    // That's it! The base FluxUIComponent class will find these attributes
    // and create the bindings automatically in Awake.
}

/// <summary>
/// A custom Value Converter that transforms a boolean value into a readable string.
/// </summary>
public class BoolToGroundedTextConverter : IValueConverter<bool, string>
{
    public string Convert(bool value) => value ? "Grounded: YES" : "Grounded: NO";
    public bool ConvertBack(string value) => value.Contains("YES");
}
```

**✅ Checkpoint 4**: Assign the Text components to the `PlayerHUD`. Press Play. All UI elements now update automatically as you move, jump, and (soon) collect gold!

---

## 5. Adding Collectible Items (Interaction)

Collectibles call the central logic in our `InventoryManager`. They don't know about the inventory data or the UI.

### Step 5.1: Create a Collectible Script
Create a `FluxMonoBehaviour` named `Collectible.cs`.
```csharp
using UnityEngine;

public class Collectible : FluxMonoBehaviour
{
    [SerializeField] private int goldValue = 10;
    private static InventoryManager _inventoryManager; // Cache for performance

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_inventoryManager == null)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
        }

        if (_inventoryManager != null)
        {
            // The collectible calls the central business logic.
            _inventoryManager.AddGold(goldValue);
            Destroy(gameObject);
        }
    }
}
```

**✅ Checkpoint 5**: Create a "GoldCoin" prefab, attach the `Collectible` script, and place it in the scene. When the player touches it, your Gold Text in the UI will update instantly!

---

## 6. Connecting Systems with Events

Finally, let's add an `AudioManager` that listens for the `PlayerJumpedEvent` to show how decoupled systems can communicate.

### Step 6.1: Create the Audio Manager
Create a `FluxMonoBehaviour` named `AudioManager.cs` and place it in your scene.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class AudioManager : FluxMonoBehaviour
{
    // This attribute tells FluxFramework to automatically subscribe this method
    // to any PlayerJumpedEvent published on the EventBus.
    [FluxEventHandler]
    private void OnPlayerJumped(PlayerJumpedEvent evt)
    {
        // In a real game, you would play an audio clip here.
        Debug.Log("AudioManager heard PlayerJumpedEvent! Playing jump sound.");
    }
}
```

**🎉 Congratulations!** You have now built a small, complete game loop using FluxFramework. Notice the clean separation:
*   **PlayerController (Logic):** Manages player physics and state.
*   **PlayerInventory (Data):** Holds persistent data.
*   **InventoryManager (Logic):** Manages inventory rules.
*   **Collectible (Interaction):** Triggers logic in other systems.
*   **PlayerHUD (View):** Displays data it's bound to.
*   **AudioManager (Feedback):** Reacts to events.

None of these components hold direct references to each other, making the system easy to change, expand, and debug. This is the power of reactive architecture.