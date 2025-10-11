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
    public class FluxComponentRegistry : IFluxComponentRegistry
    {
        private Dictionary<Type, FluxComponentAttribute> _discoveredComponents = new Dictionary<Type, FluxComponentAttribute>();
        private Dictionary<string, List<Type>> _componentsByCategory = new Dictionary<string, List<Type>>();
        private HashSet<Type> _registeredTypes = new HashSet<Type>();
        private HashSet<MonoBehaviour> _registeredInstances = new HashSet<MonoBehaviour>();
        private bool _isDiscovered = false;
        private bool _isInitialized = false;

        private readonly IFluxManager _manager;

        /// <summary>
        /// Event raised when a component type is discovered and registered with the registry.
        /// </summary>
        public event Action<Type, FluxComponentAttribute> OnComponentTypeRegistered;

        /// <summary>
        /// Event raised when an instance of a component is registered with the framework.
        /// </summary>
        public event Action<MonoBehaviour, FluxComponentAttribute> OnComponentInstanceRegistered;

        public FluxComponentRegistry(IFluxManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        /// <summary>
        /// Initializes the component registry and discovers all FluxComponent types in the project.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            DiscoverComponentTypes();
            _isInitialized = true;
            _manager.Logger.Info($"[FluxFramework] Component Registry initialized with {_discoveredComponents.Count} component types.");

        }

        /// <summary>
        /// Scans all loaded assemblies for types marked with the [FluxComponent] attribute.
        /// </summary>
        private void DiscoverComponentTypes()
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
                    FluxFramework.Core.Flux.Manager.Logger.Warning($"[FluxFramework] An error occurred while scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }
            _isDiscovered = true;
        }

        /// <summary>
        /// Registers a specific instance of a MonoBehaviour with the framework.
        /// This is the master controller for component initialization.
        /// </summary>
        public void RegisterComponentInstance(MonoBehaviour component)
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

            // 1. Delegate reactive property creation to the component itself via the IFluxReactiveObject interface.
            // This decouples the registry from the property factory.
            if (component is IFluxReactiveObject reactiveObject)
            {
                reactiveObject.InitializeReactiveProperties(_manager);
            }

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
        private void CallRegistrationMethods(MonoBehaviour component, FluxComponentAttribute attribute)
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
                    FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Error calling registration method {method.Name} on {component.GetType().Name}: {ex.Message}", component);
                }
            }
        }

        /// <summary>
        /// Scans the component for methods marked with [FluxPropertyChangeHandler] and subscribes them.
        /// </summary>
        private void RegisterPropertyChangeHandlers(MonoBehaviour component)
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
        private IDisposable SubscribeHandlerToProperty(MonoBehaviour component, MethodInfo method, FluxPropertyChangeHandlerAttribute attribute)
        {
            try
            {
                var parameters = method.GetParameters();
                
                if (parameters.Length > 2)
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Method '{method.Name}' on '{component.GetType().Name}' has too many parameters for a [FluxPropertyChangeHandler]. It can have 0, 1 (newValue), or 2 (oldValue, newValue) parameters.", component);
                    return null;
                }

                return Flux.Manager.Properties.SubscribeDeferred(attribute.PropertyKey, (property) =>
                {
                    try
                    {
                        var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                        IDisposable subscription = null;
                        
                        if (parameters.Length == 2) // Signature: OnChange(T oldValue, T newValue)
                        {
                            // Correctly search for the method with TWO parameters: Action<T, T> and bool
                            var actionType = typeof(Action<,>).MakeGenericType(property.ValueType, property.ValueType);
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { actionType, typeof(bool) });

                            if (subscribeMethod != null)
                            {
                                var handlerDelegate = Delegate.CreateDelegate(actionType, component, method);
                                // Invoke with TWO arguments: the delegate and the 'fireOnSubscribe' boolean
                                subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate, true });
                            }
                        }
                        else if (parameters.Length == 1) // Signature: OnChange(T newValue)
                        {
                            // Correctly search for the method with TWO parameters: Action<T> and bool
                            var actionType = typeof(Action<>).MakeGenericType(property.ValueType);
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { actionType, typeof(bool) });

                            if (subscribeMethod != null)
                            {
                                var handlerDelegate = Delegate.CreateDelegate(actionType, component, method);
                                // Invoke with TWO arguments: the delegate and the 'fireOnSubscribe' boolean
                                subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate, true });
                            }
                        }
                        else // Signature: OnChange()
                        {
                            // This one is simpler as it uses the non-generic Subscribe(Action<object>)
                            subscription = property.Subscribe(_ => method.Invoke(component, null), true);
                        }

                        if (subscription != null)
                        {
                            subscriptionManager.Add(subscription);
                        }
                        else
                        {
                            FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Could not find a suitable 'Subscribe' method on property '{attribute.PropertyKey}' for handler '{method.Name}'. Check for API changes in ReactiveProperty.", component);
                        }
                    }
                    catch (Exception ex)
                    {
                        FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Error attaching property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}. Check method signature.", component);
                    }
                });
            }
            catch (Exception ex)
            {
                FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Error subscribing property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}", component);
                return null;
            }
        }
        
        /// <summary>
        /// Scans the component for methods marked with [FluxEventHandler] and subscribes them to the EventBus.
        /// </summary>
        private void RegisterEventHandlers(MonoBehaviour component)
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
        private void RegisterEventHandler(MonoBehaviour component, MethodInfo method, FluxEventHandlerAttribute attribute)
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
                        FluxFramework.Core.Flux.Manager.Logger.Warning($"[FluxFramework] Cannot automatically register event handler '{method.Name}' on '{component.GetType().Name}'. Method has more than one parameter. Use [FluxEventHandler(typeof(MyEvent))] on a method with zero or one parameter.", component);
                        return;
                    }
                }

                if (eventType == null)
                {
                    FluxFramework.Core.Flux.Manager.Logger.Warning($"[FluxFramework] Could not determine event type for handler '{method.Name}' on '{component.GetType().Name}'. Ensure the method has one parameter that inherits from IFluxEvent, or specify the type in the attribute.", component);
                    return;
                }

                // --- Step 2: Find the correct 'Subscribe' method overload using reflection ---
                // We need to find the generic Subscribe<T>(Action<T> handler, int priority) method.
                var subscribeMethodInfo = typeof(IEventBus)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => 
                        m.Name == "Subscribe" && 
                        m.IsGenericMethodDefinition &&
                        m.GetParameters().Length == 2 && 
                        m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Action<>) &&
                        m.GetParameters()[1].ParameterType == typeof(int));

                if (subscribeMethodInfo == null)
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error("[FluxFramework] Critical Error: Could not find the required 'IEventBus.Subscribe<T>(Action<T>, int)' method via reflection.", component);
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
                genericSubscribeMethod.Invoke(_manager.EventBus, new object[] { eventHandlerDelegate, priority });

                FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Registered EventHandler '{method.Name}' for event '{eventType.Name}' with priority {priority} on '{component.GetType().Name}'.", component);
            }
            catch (Exception ex)
            {
                // This will catch errors from Delegate.CreateDelegate (if signatures mismatch) or Invoke.
                FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Error registering EventHandler '{method.Name}' on '{component.GetType().Name}': {ex.Message}. Please ensure the method signature matches the event type.", component);
            }
        }

        /// <summary>
        /// Registers all qualifying MonoBehaviours in the currently active scene.
        /// </summary>
        public void RegisterAllComponentsInScene()
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
                FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Auto-registered {registeredCount} new FluxComponent instances in scene.");
            }
        }

        /// <summary>
        /// A filter to avoid scanning system and Unity assemblies.
        /// </summary>
        private bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") || assemblyName.StartsWith("Microsoft.") || assemblyName.StartsWith("Unity.") || 
                   assemblyName.StartsWith("UnityEngine") || assemblyName.StartsWith("UnityEditor") || 
                   assemblyName.StartsWith("mscorlib") || assemblyName.StartsWith("netstandard");
        }
        
        /// <summary>
        /// Clears the cache of registered component instances.
        /// This MUST be called on scene load to prevent memory leaks.
        /// </summary>
        public void ClearInstanceCache()
        {
            _registeredInstances.Clear();
            _registeredTypes.Clear(); 
        }

        /// <summary>
        /// Clears all cached data and forces reinitialization. (For Editor use)
        /// </summary>
        public void ClearCache()
        {
            _discoveredComponents.Clear();
            _componentsByCategory.Clear();
            ClearInstanceCache();
            _isDiscovered = false;
            _isInitialized = false;
        }
    }
}