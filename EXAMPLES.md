# 🎯 FluxFramework - Practical Examples

**Ready-to-use code examples for common game development scenarios, updated for the latest architecture.**

## 📋 Table of Contents

1.  [Player Stats System](#1-player-stats-system)
2.  [Data-Driven Inventory System](#2-data-driven-inventory-system)
3.  [Player Stats with Dictionary System](#25-player-stats-with-dictionary-system)
4.  [Settings Menu with Two-Way Binding](#3-settings-menu-with-two-way-binding)
5.  [Advanced Health Bar (Manual Subscription)](#4-advanced-health-bar-manual-subscription)
6.  [Reacting to State Changes (Declarative)](#5-reacting-to-state-changes-declarative)

---

## 1. Player Stats System

This example shows how to define player stats in a central data container and have the UI react to them automatically with zero binding code.

### a) `PlayerData.cs` (The Data)
First, define all the player's state in a `FluxDataContainer`. This `ScriptableObject` is the **single source of truth**.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Flux/Examples/Player Data")]
public class PlayerData : FluxDataContainer
{
    // The [ReactiveProperty] attribute declares a global property and links it to this public field.
    // Modifying this field via helper methods will update the global state.
    [ReactiveProperty("player.health", Persistent = false)]
    [FluxRange(0, 100)]
    public float Health = 100f;

    [ReactiveProperty("player.mana", Persistent = false)]
    [FluxRange(0, 100)]
    public float Mana = 100f;
}
```
*Don't forget to create an instance of this asset in your project!*

### b) `PlayerStatsManager.cs` (The Logic)
This component's job is to contain the **business logic** that modifies the state in `PlayerData`.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using Flux; // For FluxKeys

public class PlayerStatsManager : FluxMonoBehaviour
{
    [SerializeField] private PlayerData playerData; // Assign your PlayerData asset here

    [FluxAction("Take Damage", ButtonText = "Apply 10 Damage")]
    public void TakeDamage(float amount = 10f)
    {
        // To safely modify the data, we use the helper method on the DataContainer itself.
        // This is the correct way to write to a property defined in a ScriptableObject.
        playerData.UpdateReactiveProperty(FluxKeys.PlayerHealth, currentHealth => currentHealth - amount);
    }
}
```

### c) `PlayerStatsUI.cs` (The View)
This `FluxUIComponent` is purely declarative. It displays the stats but contains **no logic**.
```csharp
using UnityEngine.UI;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Core;
using Flux; // For FluxKeys

public class PlayerStatsUI : FluxUIComponent
{
    [Header("Bindings")]
    [FluxBinding(FluxKeys.PlayerHealth)]
    [SerializeField] private Slider _healthBarSlider;

    [FluxBinding(FluxKeys.PlayerHealth, ConverterType = typeof(HealthToTextConverter))]
    [SerializeField] private TextMeshProUGUI _healthText;
}

// A simple converter to format the health float as a string.
public class HealthToTextConverter : IValueConverter<float, string>
{
    public string Convert(float value) => $"{value:F0} / 100";
    public float ConvertBack(string value) => float.TryParse(value, out var f) ? f : 0;
    
    object IValueConverter.Convert(object value) => Convert((float)value);
    object IValueConverter.ConvertBack(object value) => ConvertBack((string)value);
}
```

---

## 2. Data-Driven Inventory System

This example uses a `FluxDataContainer` to manage inventory data, completely separate from any scene logic.

### a) `InventoryData.cs` (The Data)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "InventoryData", menuName = "Flux/Examples/Inventory Data")]
public class InventoryData : FluxDataContainer
{
    [ReactiveProperty("inventory.gold", Persistent = true)]
    public int Gold;
    
    // Lists are supported directly with ReactiveProperty attribute
    // Note: Arrays are not currently supported, use List<T> instead
    [ReactiveProperty("inventory.items")]
    public List<string> Items = new List<string>();
}
```

### b) `InventoryManager.cs` (The Logic)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System.Collections.Generic;
using Flux;

public class ItemAddedEvent : FluxEventBase { public string ItemName { get; } public ItemAddedEvent(string name) { ItemName = name; } }

public class InventoryManager : FluxMonoBehaviour
{
    [SerializeField] private InventoryData inventoryData; // Assign your InventoryData asset here

    [FluxAction("Add Item")]
    public void AddItem(string itemName)
    {
        if (inventoryData == null || string.IsNullOrEmpty(itemName)) return;
        
        // Use helper methods from FluxMonoBehaviour for safe list operations
        AddToReactiveCollection<string>("inventory.items", itemName);
        
        PublishEvent(new ItemAddedEvent(itemName));
    }
    
    [FluxAction("Remove Item")]
    public void RemoveItem(string itemName)
    {
        if (inventoryData == null || string.IsNullOrEmpty(itemName)) return;
        
        // Use helper methods for safe removal
        RemoveFromReactiveCollection<string>("inventory.items", itemName);
    }
    
    [FluxAction("Clear Inventory")]
    public void ClearInventory()
    {
        if (inventoryData == null) return;
        
        // Use helper method for safe clearing
        ClearReactiveCollection<string>("inventory.items");
    }
}
```

---

## 2.5. Player Stats with Dictionary System

This example shows how to use `ReactiveDictionary` for complex key-value data that requires fine-grained change tracking.

### a) `PlayerStatsData.cs` (The Data)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.Extensions;

[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Flux/Examples/Player Stats Data")]
public class PlayerStatsData : FluxDataContainer
{
    // Dictionaries require ReactiveDictionary wrapper for change tracking
    // Note: Regular Dictionary<TKey, TValue> is not supported
    [ReactiveProperty("player.stats")]
    public ReactiveDictionary<string, int> Stats = new ReactiveDictionary<string, int>();
}
```

### b) `PlayerStatsManager.cs` (The Logic)
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.Extensions;
using Flux;

public class StatChangedEvent : FluxEventBase 
{ 
    public string StatName { get; }
    public int NewValue { get; }
    public StatChangedEvent(string statName, int newValue) { StatName = statName; NewValue = newValue; }
}

public class PlayerStatsManager : FluxMonoBehaviour
{
    [SerializeField] private PlayerStatsData playerStatsData;

    protected override void OnFluxAwake()
    {
        // Subscribe to fine-grained dictionary events
        if (playerStatsData?.Stats != null)
        {
            playerStatsData.Stats.OnItemAdded += OnStatAdded;
            playerStatsData.Stats.OnItemChanged += OnStatChanged;
            playerStatsData.Stats.OnItemRemoved += OnStatRemoved;
        }
        
        InitializeDefaultStats();
    }

    private void InitializeDefaultStats()
    {
        // Use helper methods from FluxMonoBehaviour for safe dictionary operations
        SetInReactiveDictionary<string, int>("player.stats", "Health", 100);
        SetInReactiveDictionary<string, int>("player.stats", "Mana", 50);
        SetInReactiveDictionary<string, int>("player.stats", "Strength", 10);
        SetInReactiveDictionary<string, int>("player.stats", "Agility", 8);
    }

    [FluxAction("Increase Health")]
    public void IncreaseHealth()
    {
        if (TryGetFromReactiveDictionary<string, int>("player.stats", "Health", out int currentHealth))
        {
            SetInReactiveDictionary<string, int>("player.stats", "Health", currentHealth + 10);
        }
    }

    [FluxAction("Add New Stat")]
    public void AddNewStat(string statName, int value)
    {
        if (!string.IsNullOrEmpty(statName))
        {
            AddToReactiveDictionary<string, int>("player.stats", statName, value);
        }
    }

    [FluxAction("Remove Stat")]
    public void RemoveStat(string statName)
    {
        if (!string.IsNullOrEmpty(statName))
        {
            RemoveFromReactiveDictionary<string, int>("player.stats", statName);
        }
    }

    // Fine-grained event handlers
    private void OnStatAdded(string statName, int value)
    {
        Debug.Log($"New stat added: {statName} = {value}");
        PublishEvent(new StatChangedEvent(statName, value));
    }

    private void OnStatChanged(string statName, int newValue)
    {
        Debug.Log($"Stat changed: {statName} = {newValue}");
        PublishEvent(new StatChangedEvent(statName, newValue));
    }

    private void OnStatRemoved(string statName)
    {
        Debug.Log($"Stat removed: {statName}");
    }
}
```

---

## 3. Settings Menu with Two-Way Binding

This shows how `TwoWay` binding creates a settings menu where UI controls both read and write values.

### a) `GameSettings.cs` (The Data)
Using a `FluxSettings` `ScriptableObject` is perfect for this, as it adds auto-saving features.
```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Flux/Examples/Game Settings")]
public class GameSettings : FluxSettings
{
    [ReactiveProperty("settings.musicVolume", Persistent = true)]
    [FluxRange(0f, 1f)]
    public float MusicVolume = 0.8f;
    
    [ReactiveProperty("settings.fullscreen", Persistent = true)]
    public bool Fullscreen = true;
}
```

### b) `SettingsApplier.cs` (A Logic Component)
A separate component that listens for setting changes and applies them to Unity's systems.
```csharp
using UnityEngine;
using FluxFramework.Core;
using System;
using Flux;

public class SettingsApplier : FluxMonoBehaviour
{
    private IDisposable _fullscreenSub;

    protected override void OnFluxAwake()
    {
        // Subscribe to apply the settings when they change.
        _fullscreenSub = SubscribeToProperty<bool>(FluxKeys.SettingsFullscreen, value => Screen.fullScreen = value, fireOnSubscribe: true);
        // For volume, you would subscribe and set AudioMixer values.
    }

    protected override void OnFluxDestroy()
    {
        _fullscreenSub?.Dispose();
    }
}
```

### c) `SettingsUI.cs` (The View)
```csharp
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;
using Flux;

public class SettingsUI : FluxUIComponent
{
    // The 'TwoWay' mode means that when the user moves the slider,
    // it will automatically update the "settings.musicVolume" property.
    [FluxBinding(FluxKeys.SettingsMusicVolume, Mode = BindingMode.TwoWay)]
    [SerializeField] private Slider _musicVolumeSlider;

    [FluxBinding(FluxKeys.SettingsFullscreen, Mode = BindingMode.TwoWay)]
    [SerializeField] private Toggle _fullscreenToggle;
}
```

---

## 4. Advanced Health Bar (Manual Subscription)

This shows how to use `RegisterCustomBindings` for complex logic that attributes can't handle.

```csharp
using UnityEngine;
using UnityEngine.UI;
using FluxFramework.UI;
using FluxFramework.Core;
using System.Collections;
using System;
using Flux;

public class AdvancedHealthBar : FluxUIComponent
{
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _damageFill; // A secondary image for a "damage trail" effect
    
    private IDisposable _healthSubscription;
    private Coroutine _damageAnimation;

    // We use the custom binding method for complex logic.
    protected override void RegisterCustomBindings()
    {
        // We subscribe manually using the helper method and store the IDisposable handle.
        _healthSubscription = SubscribeToProperty<float>(FluxKeys.PlayerHealth, OnHealthChanged, fireOnSubscribe: true);
        
        // We must also track this binding if we want the base class to manage its lifecycle.
        // For manual subscriptions, however, cleaning up in CleanupComponent is clearer.
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
        // ... (Coroutine logic remains the same)
    }
}
```

---

## 5. Reacting to State Changes (Declarative)

Use the `[FluxPropertyChangeHandler]` attribute for a clean way to run logic when a property changes, without needing manual subscriptions.

```csharp
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using Flux;

public class PlayerFeedback : FluxMonoBehaviour
{
    // This attribute tells the framework to automatically subscribe this method
    // to the "player.health" property. The framework handles cleanup.
    [FluxPropertyChangeHandler(FluxKeys.PlayerHealth)]
    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (newValue < oldValue)
        {
            Debug.Log("Player took damage! Playing feedback effect.");
            // Play a damage vignette, sound effect, etc.
        }
    }
}
```