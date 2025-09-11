using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// System that manages reactive bindings between data and UI components.
    /// It handles subscription lifecycles, and optional features like debouncing 
    /// for UI updates based on BindingOptions.
    /// </summary>
    public static class ReactiveBindingSystem
    {
        // --- REGISTRIES ---

        // Main registry of active bindings, categorized by property key.
        private static readonly Dictionary<string, List<IUIBinding>> _bindings = new Dictionary<string, List<IUIBinding>>();

        // Tracks the IDisposable subscription for each binding to ensure proper cleanup.
        private static readonly Dictionary<IUIBinding, IDisposable> _subscriptions = new Dictionary<IUIBinding, IDisposable>();
        
        // Tracks active debouncing coroutines for each binding to manage update delays.
        private static readonly Dictionary<IUIBinding, Coroutine> _debounceCoroutines = new Dictionary<IUIBinding, Coroutine>();
        
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the reactive binding system.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        /// <summary>
        /// Creates a binding between a reactive property and a UI component using specified options.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="propertyKey">The unique key of the reactive property.</param>
        /// <param name="binding">The UI binding implementation (e.g., TextBinding, SliderBinding).</param>
        /// <param name="options">Configuration for the binding, such as mode, delay, and value converter.</param>
        public static void Bind<T>(string propertyKey, IUIBinding<T> binding, BindingOptions options = null)
        {
            options = options ?? BindingOptions.Default;

            if (string.IsNullOrEmpty(propertyKey))
            {
                Debug.LogError("[FluxFramework] Cannot create a binding with a null or empty property key.", binding.Component);
                return;
            }

            if (!_bindings.ContainsKey(propertyKey))
            {
                _bindings[propertyKey] = new List<IUIBinding>();
            }

            if (_bindings[propertyKey].Contains(binding)) return; // Avoid registering the same binding twice.

            _bindings[propertyKey].Add(binding);

            if (binding is UIBinding<T> baseBinding) // Check if it's our base type
            {
                baseBinding.SetOptions(options);
            }

            var property = FluxManager.Instance.GetOrCreateProperty<T>(propertyKey, default(T));

            // Define the update action that will be called when the property changes.
            if (options.ImmediateUpdate)
            {
                Action<T> updateAction = value =>
                {
                    try
                    {
                        // 1. Apply a value converter if one is provided.
                        object finalValue = options.Converter != null ? options.Converter.Convert(value) : value;

                        // 2. Apply debouncing if a delay is specified.
                        if (options.UpdateDelayMs > 0)
                        {
                            EnqueueDebouncedUpdate(binding, (T)finalValue, options.UpdateDelayMs);
                        }
                        else
                        {
                            binding.UpdateUI((T)finalValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FluxFramework] Error updating UI for binding '{propertyKey}': {ex.Message}", binding.Component);
                    }
                };

                // Subscribe to the property and store the IDisposable subscription for cleanup.
                IDisposable subscription = property.Subscribe(updateAction);
                _subscriptions[binding] = subscription;
            }
            
            
            binding.UpdateFromProperty();
        }

        /// <summary>
        /// Unbinds all bindings associated with a specific property key.
        /// </summary>
        public static void Unbind(string propertyKey)
        {
            if (_bindings.TryGetValue(propertyKey, out var bindingList))
            {
                // Iterate over a copy of the list to safely modify the collection while iterating.
                foreach (var binding in bindingList.ToList())
                {
                    Unbind(propertyKey, binding);
                }
            }
        }

        /// <summary>
        /// Unbinds a specific binding and cleans up all its associated resources.
        /// </summary>
        public static void Unbind(string propertyKey, IUIBinding binding)
        {
            if (binding == null) return;

            // 1. Stop any pending debounced update coroutine.
            if (_debounceCoroutines.TryGetValue(binding, out Coroutine coroutine))
            {
                if (coroutine != null && FluxManager.Instance != null)
                {
                    FluxManager.Instance.StopCoroutine(coroutine);
                }
                _debounceCoroutines.Remove(binding);
            }

            // 2. Unsubscribe from the ReactiveProperty by disposing the stored subscription.
            if (_subscriptions.TryGetValue(binding, out IDisposable subscription))
            {
                subscription.Dispose();
                _subscriptions.Remove(binding);
            }

            // 3. Remove the binding from the central registry.
            if (_bindings.TryGetValue(propertyKey, out List<IUIBinding> bindingList))
            {
                bindingList.Remove(binding);
                if (bindingList.Count == 0)
                {
                    _bindings.Remove(propertyKey);
                }
            }

            // 4. Dispose the binding itself, allowing it to perform internal cleanup (e.g., remove UI event listeners).
            binding.Dispose();
        }

        /// <summary>
        /// Clears all bindings and their resources. Typically called during scene transitions or application shutdown.
        /// </summary>
        public static void ClearAll()
        {
            // Iterate over a copy of the keys to safely unbind all registered bindings.
            foreach (var propertyKey in _bindings.Keys.ToList())
            {
                var bindingList = _bindings[propertyKey];
                foreach (var binding in bindingList.ToList())
                {
                    Unbind(propertyKey, binding);
                }
            }
            
            // Ensure all collections are completely empty after cleanup.
            _bindings.Clear();
            _subscriptions.Clear();
            _debounceCoroutines.Clear();
        }

        /// <summary>
        /// Manually triggers a UI update for all bindings associated with a property key.
        /// This is useful for bindings created with ImmediateUpdate = false.
        /// </summary>
        public static void RefreshBindings(string propertyKey)
        {
            if (_bindings.TryGetValue(propertyKey, out var bindingList))
            {
                var property = FluxManager.Instance.GetProperty(propertyKey);
                if (property == null) return;
                
                var value = property.GetValue();

                foreach (var binding in bindingList)
                {
                    binding.UpdateFromProperty();
                }
            }
        }
        
        /// <summary>
        /// Gets the total number of active UI bindings.
        /// </summary>
        public static int GetActiveBindingCount()
        {
            if (!_isInitialized) return 0;
            // The .Sum() extension method requires 'using System.Linq;'
            return _bindings.Values.Sum(list => list.Count);
        }

        /// <summary>
        /// Manages the debouncing logic by starting a delayed update coroutine.
        /// Cancels any previously pending update for the same binding.
        /// </summary>
        private static void EnqueueDebouncedUpdate<T>(IUIBinding<T> binding, T value, int delayMs)
        {
            if (FluxManager.Instance == null)
            {
                binding.UpdateUI(value); // Fallback to immediate update if the manager is not available.
                return;
            }

            // If an update is already pending for this binding, cancel it.
            if (_debounceCoroutines.TryGetValue(binding, out Coroutine existingCoroutine))
            {
                if (existingCoroutine != null)
                {
                    FluxManager.Instance.StopCoroutine(existingCoroutine);
                }
            }
            
            // Start a new coroutine for the delayed update.
            var newCoroutine = FluxManager.Instance.StartCoroutine(DelayedUpdateCoroutine(binding, value, delayMs));
            _debounceCoroutines[binding] = newCoroutine;
        }
        
        /// <summary>
        /// A coroutine that waits for a specified delay before applying a value to a UI binding.
        /// </summary>
        private static IEnumerator DelayedUpdateCoroutine<T>(IUIBinding<T> binding, T value, int delayMs)
        {
            yield return new WaitForSeconds(delayMs / 1000f);
            
            // It's possible the binding's component was destroyed during the delay.
            // This check prevents NullReferenceExceptions.
            if (binding != null && binding.Component != null)
            {
                binding.UpdateUI(value);
            }

            // Clean up the coroutine reference once it has finished executing.
            _debounceCoroutines.Remove(binding);
        }
    }
}