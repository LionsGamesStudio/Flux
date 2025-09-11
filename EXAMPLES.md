# 🎯 FluxFramework - Practical Examples

**Ready-to-use code examples for common game development scenarios.**

## 📋 Table of Contents

1.  [Player Stats System](#1-player-stats-system)
2.  [Data-Driven Inventory System](#2-data-driven-inventory-system)
3.  [Settings Menu with Two-Way Binding](#3-settings-menu-with-two-way-binding)
4.  [Advanced Health Bar (Custom Binding)](#4-advanced-health-bar-custom-binding)
5.  [VR Interaction](#5-vr-interaction)

---

## 1. Player Stats System

This example shows how to define player stats, modify them, and have the UI react automatically.

### a) `PlayerStats.cs` (The Logic)
This component manages the player's health. It inherits from `FluxMonoBehaviour`.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerStats : FluxMonoBehaviour
{
    // The [ReactiveProperty] attribute registers a global property linked to this field.
    // The [FluxRange] attribute adds automatic validation in the editor and at runtime.
    [ReactiveProperty("player.health")]
    [FluxRange(0, 100)]
    private float _health = 100f;

    [FluxButton("Take 10 Damage")]
    private void TakeDamage()
    {
        // To modify the property, get it from the manager and set its .Value
        var healthProp = FluxManager.Instance.GetProperty<float>("player.health");
        if (healthProp != null)
        {
            healthProp.Value = Mathf.Max(0, healthProp.Value - 10);
        }
    }
}
```

### b) `PlayerStatsUI.cs` (The View)
This `FluxUIComponent` displays the stats. It contains **zero logic**.
```csharp
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

public class PlayerStatsUI : FluxUIComponent
{
    // The [FluxBinding] attribute on the component field is all that's needed.
    // The base class automatically finds this and creates the binding.
    [Header("Bindings")]
    [FluxBinding("player.health")]
    [SerializeField] private Slider _healthBarSlider;

    [FluxBinding("player.health")]
    [SerializeField] private TMPro.TextMeshProUGUI _healthText;
}
```
**Setup:**
1.  Attach `PlayerStats` to your player GameObject.
2.  Attach `PlayerStatsUI` to a UI panel.
3.  Assign a `Slider` and a `TextMeshProUGUI` to the fields in the `PlayerStatsUI` inspector.
4.  Press Play. The UI will now track the player's health automatically. Use the "Take 10 Damage" button in the `PlayerStats` inspector to test it.

---

## 2. Data-Driven Inventory System

This example uses a `FluxDataContainer` to manage inventory data, completely separate from any scene.

### a) `PlayerInventory.cs` (The Data)
A `ScriptableObject` that defines the inventory's data structure.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Flux/Examples/Player Inventory")]
public class PlayerInventory : FluxDataContainer
{
    [ReactiveProperty("inventory.gold")]
    public int Gold;

    [ReactiveProperty("inventory.items")]
    public List<string> Items = new List<string>();
}
```

### b) `InventoryManager.cs` (The Logic)
A `FluxMonoBehaviour` that contains the business logic for manipulating the inventory.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System.Collections.Generic;

// An event to announce when an item is added.
public class ItemAddedEvent : FluxEventBase { public string ItemName { get; } public ItemAddedEvent(string name) { ItemName = name; } }

public class InventoryManager : FluxMonoBehaviour
{
    [SerializeField] private PlayerInventory inventoryData;

    [FluxAction("Add Item")]
    public void AddItem(string itemName)
    {
        if (inventoryData == null || string.IsNullOrEmpty(itemName)) return;

        // Get a mutable copy of the list.
        var currentItems = new List<string>(inventoryData.Items);
        currentItems.Add(itemName);
        
        // Assign the new list back to the property. This triggers the reactive update.
        inventoryData.Items = currentItems;
        
        // Publish an event to notify other systems.
        PublishEvent(new ItemAddedEvent(itemName));
    }
}
```

### c) `InventoryUI.cs` (The View)
This UI component is purely declarative.
```csharp
using UnityEngine;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;

public class InventoryUI : FluxUIComponent
{
    [FluxBinding("inventory.gold")]
    [SerializeField] private TextMeshProUGUI _goldText;
    
    // Note: Binding directly to a List<string> to populate a UI requires
    // a custom binding in RegisterCustomBindings() or a specialized FluxCollectionUI component.
    // For simplicity, we can listen to the ItemAddedEvent.
    
    // See the Advanced Health Bar example for custom registration.
}
```

---

## 3. Settings Menu with Two-Way Binding

This shows how to use `TwoWay` binding to create a settings menu where UI controls both read and write values.

### a) `GameSettings.cs` (The Logic)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System;

public class GameSettings : FluxMonoBehaviour
{
    [ReactiveProperty("settings.musicVolume")]
    [FluxRange(0f, 1f)]
    private float _musicVolume = 0.8f;
    
    [ReactiveProperty("settings.fullscreen")]
    private bool _fullscreen = true;

    private IDisposable _fullscreenSub;

    protected override void Awake()
    {
        base.Awake();
        // Subscribe to apply the setting when it changes.
        var fullscreenProp = FluxManager.Instance.GetProperty<bool>("settings.fullscreen");
        _fullscreenSub = fullscreenProp.Subscribe(value => Screen.fullScreen = value);
    }

    protected override void OnDestroy()
    {
        _fullscreenSub?.Dispose();
        base.OnDestroy();
    }
}
```

### b) `SettingsUI.cs` (The View)
```csharp
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

public class SettingsUI : FluxUIComponent
{
    // The 'TwoWay' mode means that when the user moves the slider,
    // it will automatically update the "settings.musicVolume" property.
    [FluxBinding("settings.musicVolume", Mode = BindingMode.TwoWay)]
    [SerializeField] private Slider _musicVolumeSlider;

    [FluxBinding("settings.fullscreen", Mode = BindingMode.TwoWay)]
    [SerializeField] private Toggle _fullscreenToggle;
}
```

---

## 4. Advanced Health Bar (Custom Binding)

This example shows how to use `RegisterCustomBindings` for logic that is too complex for attributes alone.

```csharp
using UnityEngine;
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Core;
using System.Collections;
using System;

public class AdvancedHealthBar : FluxUIComponent
{
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _damageFill; // A secondary image for a "damage trail" effect
    [SerializeField] private TMPro.TextMeshProUGUI _healthText;

    private IDisposable _healthSubscription;
    private Coroutine _damageAnimation;

    /// <summary>
    /// We override RegisterCustomBindings for our complex, multi-component logic.
    /// </summary>
    protected override void RegisterCustomBindings()
    {
        var healthProp = FluxManager.Instance.GetOrCreateProperty<float>("player.health", 100f);
        
        // Subscribe to the property and store the handle for cleanup.
        _healthSubscription = healthProp.Subscribe(OnHealthChanged);
        
        // Initial update
        OnHealthChanged(healthProp.Value);
    }

    /// <summary>
    /// The base class will automatically clean up all tracked bindings,
    /// but since we created a subscription manually, we should clean it up manually.
    /// </summary>
    protected override void CleanupComponent()
    {
        _healthSubscription?.Dispose();
    }

    private void OnHealthChanged(float newHealth)
    {
        _healthText.text = $"{newHealth:F0} / 100";
        _healthFill.fillAmount = newHealth / 100f;
        
        // Start the damage trail animation
        if (gameObject.activeInHierarchy)
        {
            if (_damageAnimation != null) StopCoroutine(_damageAnimation);
            _damageAnimation = StartCoroutine(AnimateDamageFill());
        }
    }

    private IEnumerator AnimateDamageFill()
    {
        float timer = 0f;
        float duration = 0.5f;
        float startFill = _damageFill.fillAmount;
        float endFill = _healthFill.fillAmount;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            _damageFill.fillAmount = Mathf.Lerp(startFill, endFill, timer / duration);
            yield return null;
        }
        _damageFill.fillAmount = endFill;
    }
}
```

---

## 5. VR Interaction

The VR extension integrates seamlessly.

### a) `VRInteractionManager.cs` (The Logic)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events; // The namespace for VR events

public class VRInteractionManager : FluxMonoBehaviour
{
    [FluxEventHandler]
    private void OnObjectGrabbed(VRObjectGrabbedEvent evt)
    {
        Debug.Log($"Player grabbed {evt.GrabbedObject.name} with {evt.ControllerNode}");
        
        // Add a glowing outline to the grabbed object
        var outline = evt.GrabbedObject.AddComponent<Outline>();
        outline.OutlineColor = Color.cyan;
    }
    
    [FluxEventHandler]
    private void OnObjectReleased(VRObjectReleasedEvent evt)
    {
        Debug.Log($"Player released {evt.ReleasedObject.name}");
        if (evt.ReleasedObject.TryGetComponent<Outline>(out var outline))
        {
            Destroy(outline);
        }
    }
}
```
**Setup:** Place this `VRInteractionManager` in your VR scene. It will automatically listen for grab/release events from the `FluxVRPlayer` and add/remove an outline effect.