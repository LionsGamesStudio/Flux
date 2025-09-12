# 🎯 FluxFramework - Practical Examples

**Ready-to-use code examples for common game development scenarios, updated for the latest architecture.**

## 📋 Table of Contents

1.  [Player Stats System](#1-player-stats-system)
2.  [Data-Driven Inventory System](#2-data-driven-inventory-system)
3.  [Settings Menu with Two-Way Binding](#3-settings-menu-with-two-way-binding)
4.  [Advanced Health Bar (Custom Logic)](#4-advanced-health-bar-custom-logic)
5.  [Reacting to State Changes](#5-reacting-to-state-changes)

---

## 1. Player Stats System

This example shows how to define player stats in a logic component and have the UI react to them automatically with zero binding code.

### a) `PlayerStats.cs` (The Logic)
This component manages the player's health and is the **source of truth**.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerStats : FluxMonoBehaviour
{
    // The [ReactiveProperty] attribute declares a global property named "player.health"
    // and links it to this private field, which acts as a read-only cache.
    [ReactiveProperty("player.health")]
    [FluxRange(0, 100)] // Provides editor and runtime validation.
    private float _health = 100f;

    [FluxAction("Take Damage", ButtonText = "Apply 10 Damage")]
    public void TakeDamage(float amount = 10f)
    {
        // To modify the state, use the UpdateReactiveProperty helper method.
        // This is the only correct way to write to a property.
        UpdateReactiveProperty("player.health", _health - amount);
    }
}
```

### b) `PlayerStatsUI.cs` (The View)
This `FluxUIComponent` is purely declarative. It displays the stats but contains **no logic**.
```csharp
using UnityEngine.UI;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

public class PlayerStatsUI : FluxUIComponent
{
    // The base FluxUIComponent automatically finds these [FluxBinding] attributes
    // and creates the necessary bindings.
    
    [Header("Bindings")]
    [FluxBinding("player.health")]
    [SerializeField] private Slider _healthBarSlider;

    // We can bind multiple UI elements to the same property.
    [FluxBinding("player.health", ConverterType = typeof(HealthToTextConverter))]
    [SerializeField] private TextMeshProUGUI _healthText;
}

// A simple converter to format the health float as a string.
public class HealthToTextConverter : IValueConverter
{
    public object Convert(object value) => $"{value:F0} / 100";
    public object ConvertBack(object value) => float.TryParse(value.ToString(), out var f) ? f : 0;
}
```

---

## 2. Data-Driven Inventory System

This example uses a `FluxDataContainer` to manage inventory data, completely separate from any scene logic.

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
    
    // For lists, the property itself must be replaced for the change to be detected.
    [ReactiveProperty("inventory.items")]
    public List<string> Items = new List<string>();
}
```

### b) `InventoryManager.cs` (The Logic)
A `FluxMonoBehaviour` that contains the business logic for the inventory.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System.Collections.Generic;

public class ItemAddedEvent : FluxEventBase { public string ItemName { get; } public ItemAddedEvent(string name) { ItemName = name; } }

public class InventoryManager : FluxMonoBehaviour
{
    [SerializeField] private PlayerInventory inventoryData;

    [FluxAction("Add Item")]
    public void AddItem(string itemName)
    {
        if (inventoryData == null || string.IsNullOrEmpty(itemName)) return;
        
        var currentItems = new List<string>(inventoryData.Items); // Create a mutable copy
        currentItems.Add(itemName);
        inventoryData.Items = currentItems; // Assign the new list back to trigger the update
        
        PublishEvent(new ItemAddedEvent(itemName));
    }
}
```

---

## 3. Settings Menu with Two-Way Binding

This shows how `TwoWay` binding creates a settings menu where UI controls both read and write values.

### a) `GameSettings.cs` (The Logic)
This component holds the state for the game settings.
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
}```

### b) `SettingsApplier.cs` (The "Controller")
A separate component that listens for setting changes and applies them.
```csharp
public class SettingsApplier : FluxMonoBehaviour
{
    private IDisposable _fullscreenSub, _qualitySub;

    protected override void OnFluxAwake()
    {
        // Subscribe to apply the settings when they change.
        _fullscreenSub = SubscribeToProperty<bool>("settings.fullscreen", value => Screen.fullScreen = value);
        // Add more subscriptions for other settings like quality, volume, etc.
    }

    protected override void OnFluxDestroy()
    {
        _fullscreenSub?.Dispose();
        _qualitySub?.Dispose();
    }
}
```

### c) `SettingsUI.cs` (The View)
```csharp
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

public class SettingsUI : FluxUIComponent
{
    // The 'TwoWay' mode means that when the user moves the slider,
    // it will automatically update the "settings.musicVolume" property.
    [FluxBinding(Mode = BindingMode.TwoWay)]
    [SerializeField] private Slider _musicVolumeSlider;

    [FluxBinding(Mode = BindingMode.TwoWay)]
    [SerializeField] private Toggle _fullscreenToggle;
}
```

---

## 4. Advanced Health Bar (Custom Logic)

This shows how to use `RegisterCustomBindings` when the automatic attribute system isn't enough.

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
    
    private IDisposable _healthSubscription;
    private Coroutine _damageAnimation;

    // We use the custom binding method for complex, multi-component logic.
    protected override void RegisterCustomBindings()
    {
        var healthProp = FluxManager.Instance.GetOrCreateProperty<float>("player.health", 100f);
        
        // We subscribe manually and store the IDisposable handle.
        _healthSubscription = healthProp.Subscribe(OnHealthChanged, fireOnSubscribe: true);
    }

    // We use the custom cleanup method to dispose of our manual subscription.
    protected override void CleanupComponent()
    {
        _healthSubscription?.Dispose();
    }

    private void OnHealthChanged(float newHealth)
    {
        _healthFill.fillAmount = newHealth / 100f;
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
    }
}
```

---

## 5. Reacting to State Changes

Use the `[FluxPropertyChangeHandler]` attribute for a clean, declarative way to run logic when a property changes, without needing manual subscriptions.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

public class PlayerFeedback : FluxMonoBehaviour
{
    // This attribute tells the framework to automatically subscribe this method
    // to the "player.health" property. The framework handles cleanup.
    [FluxPropertyChangeHandler("player.health")]
    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (newValue < oldValue)
        {
            Debug.Log("Player took damage! Playing feedback effect.");
            // Play a damage vignette, sound effect, etc.
        }
    }
}