using System;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Binding.Attributes;
using UnityEngine;

namespace FluxFramework.Binding
{
    /// <summary>
    /// A factory that discovers and creates UI binding instances using reflection.
    /// It scans for classes marked with the [BindingFor] attribute to build a registry of binding creators.
    /// </summary>
    public class BindingFactory : IBindingFactory
    {
        // Maps a Component Type (e.g., typeof(Slider)) to a function that can create the appropriate binding.
        private readonly Dictionary<Type, Func<string, Component, IUIBinding>> _bindingCreators = new();
        private bool _isInitialized = false;

        /// <summary>
        /// Initializes the factory by scanning assemblies for binding types.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _bindingCreators.Clear();
            
            // Scan all loaded assemblies for IUIBinding implementations
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // Skip types that are not valid binding classes
                        if (type.IsAbstract || type.IsInterface || !typeof(IUIBinding).IsAssignableFrom(type))
                            continue;

                        // Find our custom attribute
                        var attribute = type.GetCustomAttribute<BindingForAttribute>();
                        if (attribute != null)
                        {
                            var componentType = attribute.ComponentType;

                            // Store a creator function that knows how to instantiate this binding type
                            _bindingCreators[componentType] = (propertyKey, component) =>
                            {
                                try
                                {
                                    // Assumes the binding has a constructor like: new MyBinding(string propertyKey, ComponentType component)
                                    return (IUIBinding)Activator.CreateInstance(type, propertyKey, component);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"[FluxFramework] Failed to create binding of type '{type.Name}' for component '{component.name}'. Ensure it has a constructor that accepts (string, {componentType.Name}).\n{ex.Message}", component);
                                    return null;
                                }
                            };
                        }
                    }
                }
                catch
                {
                    // Silently ignore assemblies that can't be loaded (common in Unity Editor)
                }
            }
            
            _isInitialized = true;
            Debug.Log($"[FluxFramework] BindingFactory initialized. Discovered {_bindingCreators.Count} binding types.");
        }

        /// <summary>
        /// Creates an appropriate IUIBinding instance for the given UI component.
        /// </summary>
        public IUIBinding Create(string propertyKey, Component uiComponent)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[FluxFramework] BindingFactory is being used before it was initialized. Initializing now.");
                Initialize();
            }

            if (uiComponent != null && _bindingCreators.TryGetValue(uiComponent.GetType(), out var creator))
            {
                return creator(propertyKey, uiComponent);
            }
            
            return null; // No binding creator found for this component type
        }
    }
}