using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.UI;

namespace FluxFramework.Core
{
    /// <summary>
    /// Discovers and registers MonoBehaviours marked with the [FluxComponent] attribute.
    /// It orchestrates the initialization of properties, event handlers, and UI bindings for each component.
    /// </summary>
    public static class FluxComponentRegistry
    {
        private static Dictionary<Type, FluxComponentAttribute> _discoveredComponents = new Dictionary<Type, FluxComponentAttribute>();
        private static Dictionary<string, List<Type>> _componentsByCategory = new Dictionary<string, List<Type>>();
        private static HashSet<Type> _registeredTypes = new HashSet<Type>();
        private static HashSet<MonoBehaviour> _registeredInstances = new HashSet<MonoBehaviour>();
        private static bool _isDiscovered = false;
        private static bool _isInitialized = false;

        /// <summary>
        /// Event raised when a component type is discovered and registered with the registry.
        /// </summary>
        public static event Action<Type, FluxComponentAttribute> OnComponentTypeRegistered;

        /// <summary>
        /// Event raised when an instance of a component is registered with the framework.
        /// </summary>
        public static event Action<MonoBehaviour, FluxComponentAttribute> OnComponentInstanceRegistered;

        /// <summary>
        /// Initializes the component registry and discovers all FluxComponent types in the project.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            DiscoverComponentTypes();
            _isInitialized = true;
            Debug.Log($"[FluxFramework] Component Registry initialized with {_discoveredComponents.Count} component types.");
        }

        /// <summary>
        /// Scans all loaded assemblies for types marked with the [FluxComponent] attribute.
        /// </summary>
        private static void DiscoverComponentTypes()
        {
            if (_isDiscovered) return;
            _discoveredComponents.Clear();
            _componentsByCategory.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (IsSystemAssembly(assembly.FullName)) continue;
                    var types = assembly.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract);
                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<FluxComponentAttribute>();
                        if (attribute != null)
                        {
                            _discoveredComponents[type] = attribute;
                            if (!_componentsByCategory.ContainsKey(attribute.Category))
                            {
                                _componentsByCategory[attribute.Category] = new List<Type>();
                            }
                            _componentsByCategory[attribute.Category].Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FluxFramework] An error occurred while scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }
            _isDiscovered = true;
        }

        /// <summary>
        /// Registers a specific instance of a MonoBehaviour with the framework.
        /// This is the master controller for component initialization.
        /// </summary>
        public static void RegisterComponentInstance(MonoBehaviour component)
        {
            if (component == null || _registeredInstances.Contains(component)) return;

            var type = component.GetType();
            if (!_discoveredComponents.TryGetValue(type, out var attribute))
            {
                attribute = type.GetCustomAttribute<FluxComponentAttribute>();
                if (attribute == null) return;
                _discoveredComponents[type] = attribute;
            }

            if (!attribute.AutoRegister) return;

            _registeredInstances.Add(component);
            _registeredTypes.Add(type);

            // --- REGISTRATION PIPELINE ---
            CallRegistrationMethods(component, attribute);

            // 1. Delegate all reactive property creation to the central factory.
            FluxPropertyFactory.RegisterPropertiesFor(component);

            // 2. Register event and property change handlers.
            RegisterEventHandlers(component);
            RegisterPropertyChangeHandlers(component);

            // 3. For UI components, trigger the binding process.
            // This is called last to ensure all data properties are available for binding.
            if (component is FluxUIComponent uiComponent)
            {
                uiComponent.Bind();
            }

            OnComponentInstanceRegistered?.Invoke(component, attribute);
        }

        /// <summary>
        /// Invokes any methods on the component that are marked with the [FluxOnRegister] attribute.
        /// </summary>
        private static void CallRegistrationMethods(MonoBehaviour component, FluxComponentAttribute attribute)
        {
            var methods = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<FluxOnRegisterAttribute>() != null)
                .OrderByDescending(m => m.GetCustomAttribute<FluxOnRegisterAttribute>().Priority);
            foreach (var method in methods)
            {
                try
                {
                    if (method.GetParameters().Length == 0) method.Invoke(component, null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Error calling registration method {method.Name} on {component.GetType().Name}: {ex.Message}", component);
                }
            }
        }

        /// <summary>
        /// Scans the component for methods marked with [FluxPropertyChangeHandler] and subscribes them.
        /// </summary>
        private static void RegisterPropertyChangeHandlers(MonoBehaviour component)
        {
            var methods = component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var handlerAttr = method.GetCustomAttribute<FluxPropertyChangeHandlerAttribute>();
                if (handlerAttr != null)
                {
                    var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                    IDisposable subscription = SubscribeHandlerToProperty(component, method, handlerAttr);
                    if (subscription != null)
                    {
                        subscriptionManager.Add(subscription);
                    }
                }
            }
        }

        /// <summary>
        /// A helper to create a deferred subscription for a property change handler.
        /// </summary>
        private static IDisposable SubscribeHandlerToProperty(MonoBehaviour component, MethodInfo method, FluxPropertyChangeHandlerAttribute attribute)
        {
            try
            {
                var parameters = method.GetParameters();
                
                // Validate the method signature
                if (parameters.Length > 2)
                {
                    Debug.LogError($"[FluxFramework] Method '{method.Name}' on '{component.GetType().Name}' has too many parameters for a [FluxPropertyChangeHandler]. It can have 0, 1 (newValue), or 2 (oldValue, newValue) parameters.", component);
                    return null;
                }

                // We use SubscribeDeferred. It will either run immediately or wait for the property to be created.
                return FluxManager.Instance.Properties.SubscribeDeferred(attribute.PropertyKey, (property) =>
                {
                    // This code block is executed once the property is available.
                    try
                    {
                        Type delegateType;
                        Delegate handlerDelegate;

                        if (parameters.Length == 2) // Signature: OnChange(T oldValue, T newValue)
                        {
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { typeof(Action<,>).MakeGenericType(property.ValueType, property.ValueType) });
                            delegateType = typeof(Action<,>).MakeGenericType(property.ValueType, property.ValueType);
                            handlerDelegate = Delegate.CreateDelegate(delegateType, component, method);
                            var subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate });
                            
                            // Add this final subscription to the component's cleanup manager.
                            var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                            subscriptionManager.Add(subscription);
                        }
                        else if (parameters.Length == 1) // Signature: OnChange(T newValue)
                        {
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { typeof(Action<>).MakeGenericType(property.ValueType) });
                            delegateType = typeof(Action<>).MakeGenericType(property.ValueType);
                            handlerDelegate = Delegate.CreateDelegate(delegateType, component, method);
                            var subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate });
                            
                            var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                            subscriptionManager.Add(subscription);
                        }
                        else // Signature: OnChange()
                        {
                            var subscription = property.Subscribe(_ => method.Invoke(component, null));
                            
                            var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                            subscriptionManager.Add(subscription);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FluxFramework] Error attaching property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}. Check method signature.", component);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error subscribing property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}", component);
                return null;
            }
        }
        
        /// <summary>
        /// Scans the component for methods marked with [FluxEventHandler] and subscribes them to the EventBus.
        /// </summary>
        private static void RegisterEventHandlers(MonoBehaviour component)
        {
            var methods = component.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var eventAttr = method.GetCustomAttribute<FluxEventHandlerAttribute>();
                if (eventAttr != null)
                {
                    RegisterEventHandler(component, method, eventAttr);
                }
            }
        }

        /// <summary>
        /// Registers a single event handler method with the EventBus.
        /// </summary>
        private static void RegisterEventHandler(MonoBehaviour component, MethodInfo method, FluxEventHandlerAttribute attribute)
        {
            try
            {
                var parameters = method.GetParameters();
                Type eventType = attribute.EventType;

                // --- Step 1: Determine the Event Type ---
                // If the attribute doesn't explicitly define the type, infer it from the method's first parameter.
                if (eventType == null)
                {
                    if (parameters.Length == 1 && typeof(IFluxEvent).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        eventType = parameters[0].ParameterType;
                    }
                    // Allow parameterless methods if the event type is specified in the attribute
                    else if (parameters.Length > 1)
                    {
                        Debug.LogWarning($"[FluxFramework] Cannot automatically register event handler '{method.Name}' on '{component.GetType().Name}'. Method has more than one parameter. Use [FluxEventHandler(typeof(MyEvent))] on a method with zero or one parameter.", component);
                        return;
                    }
                }

                if (eventType == null)
                {
                    Debug.LogWarning($"[FluxFramework] Could not determine event type for handler '{method.Name}' on '{component.GetType().Name}'. Ensure the method has one parameter that inherits from IFluxEvent, or specify the type in the attribute.", component);
                    return;
                }

                // --- Step 2: Find the correct 'Subscribe' method overload using reflection ---
                // We need to find the generic Subscribe<T>(Action<T> handler, int priority) method.
                var subscribeMethodInfo = typeof(EventBus)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => 
                        m.Name == "Subscribe" && 
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 2 && // Must have 2 parameters (handler, priority)
                        m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Action<>) &&
                        m.GetParameters()[1].ParameterType == typeof(int));

                if (subscribeMethodInfo == null)
                {
                    Debug.LogError("[FluxFramework] Critical Error: Could not find the required 'EventBus.Subscribe<T>(Action<T>, int)' method via reflection.", component);
                    return;
                }

                // --- Step 3: Create the generic method and the delegate ---
                var genericSubscribeMethod = subscribeMethodInfo.MakeGenericMethod(eventType);
                var delegateType = typeof(Action<>).MakeGenericType(eventType);
                
                // This will fail if the method signature does not match the event type (e.g., OnEvent(WrongType evt)).
                // The try-catch will handle this gracefully.
                var eventHandlerDelegate = Delegate.CreateDelegate(delegateType, component, method);

                // --- Step 4: Invoke the Subscribe method ---
                int priority = attribute.Priority;
                genericSubscribeMethod.Invoke(null, new object[] { eventHandlerDelegate, priority });
                
                Debug.Log($"[FluxFramework] Registered EventHandler '{method.Name}' for event '{eventType.Name}' with priority {priority} on '{component.GetType().Name}'.", component);
            }
            catch (Exception ex)
            {
                // This will catch errors from Delegate.CreateDelegate (if signatures mismatch) or Invoke.
                Debug.LogError($"[FluxFramework] Error registering EventHandler '{method.Name}' on '{component.GetType().Name}': {ex.Message}. Please ensure the method signature matches the event type.", component);
            }
        }

        /// <summary>
        /// Registers all qualifying MonoBehaviours in the currently active scene.
        /// </summary>
        public static void RegisterAllComponentsInScene()
        {
            if (!_isInitialized) Initialize();
            var allComponents = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            int registeredCount = 0;
            foreach (var component in allComponents)
            {
                var type = component.GetType();
                if (_discoveredComponents.ContainsKey(type) && !_registeredInstances.Contains(component))
                {
                    RegisterComponentInstance(component);
                    registeredCount++;
                }
            }
            if (registeredCount > 0)
            {
                Debug.Log($"[FluxFramework] Auto-registered {registeredCount} new FluxComponent instances in scene.");
            }
        }

        /// <summary>
        /// A filter to avoid scanning system and Unity assemblies.
        /// </summary>
        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") || assemblyName.StartsWith("Microsoft.") || assemblyName.StartsWith("Unity.") || 
                   assemblyName.StartsWith("UnityEngine") || assemblyName.StartsWith("UnityEditor") || 
                   assemblyName.StartsWith("mscorlib") || assemblyName.StartsWith("netstandard");
        }
        
        /// <summary>
        /// Clears the cache of registered component instances.
        /// This MUST be called on scene load to prevent memory leaks.
        /// </summary>
        public static void ClearInstanceCache()
        {
            _registeredInstances.Clear();
            _registeredTypes.Clear(); 
        }

        /// <summary>
        /// Clears all cached data and forces reinitialization. (For Editor use)
        /// </summary>
        public static void ClearCache()
        {
            _discoveredComponents.Clear();
            _componentsByCategory.Clear();
            ClearInstanceCache();
            _isDiscovered = false;
            _isInitialized = false;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Flux/Tools/Refresh Component Registry")]
        public static void EditorRefreshRegistry()
        {
            ClearCache();
            Initialize();
        }
#endif
    }
}