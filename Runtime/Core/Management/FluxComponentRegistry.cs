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
    /// It orchestrates the initialization of properties, event handlers, and UI bindings 
    /// for each component, respecting a safe, three-phase (Register, Awake, Start) lifecycle.
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
                    _manager.Logger.Warning($"[FluxFramework] An error occurred while scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }
            _isDiscovered = true;
        }

        /// <summary>
        /// Registers ALL components in the scene, respecting the Awake -> Start lifecycle.
        /// This is the main method called on scene load.
        /// </summary>
        public void RegisterAllComponentsInScene()
        {
            if (!_isInitialized) Initialize();
            
            // We use FluxMonoBehaviour as it's the class that has the lifecycle we need to manage.
            var componentsToProcess = UnityEngine.Object.FindObjectsOfType<FluxMonoBehaviour>(true)
                .Where(c => !_registeredInstances.Contains(c)).ToList();

            if (componentsToProcess.Count == 0) return;

            _manager.Logger.Info($"[FluxFramework] Starting 3-phase initialization for {componentsToProcess.Count} new components.");

            // --- PHASE 1: REGISTRATION ---
            // We register all properties, events, and bindings BEFORE any game logic runs.
            foreach (var component in componentsToProcess)
            {
                PerformRegistration(component);
            }

            // --- PHASE 2: AWAKE ---
            // Now that everything is connected, we can run the initialization logic.
            foreach (var component in componentsToProcess)
            {
                component.TriggerFluxAwake();
            }

            // --- PHASE 3: START ---
            // Once all initializations are done, we run the startup logic.
            foreach (var component in componentsToProcess)
            {
                component.TriggerFluxStart();
            }

            _manager.Logger.Info($"[FluxFramework] 3-phase initialization complete.");
        }
        
        /// <summary>
        /// Registers a single component instance (e.g., instantiated at runtime).
        /// It immediately runs the full lifecycle for this single object.
        /// </summary>
        public void RegisterComponentInstance(MonoBehaviour component)
        {
            if (component == null || _registeredInstances.Contains(component)) return;

            // Perform the registration
            PerformRegistration(component);

            // Trigger the lifecycle for this single component
            if (component is FluxMonoBehaviour fluxComponent)
            {
                fluxComponent.TriggerFluxAwake();
                fluxComponent.TriggerFluxStart();
            }
        }
        
        /// <summary>
        /// The internal registration logic for a single component (Phase 1).
        /// </summary>
        private void PerformRegistration(MonoBehaviour component)
        {
            if (component == null || _registeredInstances.Contains(component)) return;

            var type = component.GetType();
            // We only process types that have already been discovered
            if (!_discoveredComponents.TryGetValue(type, out var attribute)) return; 
            if (!attribute.AutoRegister) return;
            
            _registeredInstances.Add(component);
            _registeredTypes.Add(type);

            CallRegistrationMethods(component, attribute);

            if (component is IFluxReactiveObject reactiveObject)
            {
                reactiveObject.InitializeReactiveProperties(_manager);
            }

            RegisterEventHandlers(component);
            RegisterPropertyChangeHandlers(component);

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
                    _manager.Logger.Error($"[FluxFramework] Error calling registration method {method.Name} on {component.GetType().Name}: {ex.Message}", component);
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
                    _manager.Logger.Error($"[FluxFramework] Method '{method.Name}' on '{component.GetType().Name}' has too many parameters for a [FluxPropertyChangeHandler]. It can have 0, 1 (newValue), or 2 (oldValue, newValue) parameters.", component);
                    return null;
                }

                return _manager.Properties.SubscribeDeferred(attribute.PropertyKey, (property) =>
                {
                    try
                    {
                        var subscriptionManager = component.GetComponent<ComponentSubscriptionManager>() ?? component.gameObject.AddComponent<ComponentSubscriptionManager>();
                        IDisposable subscription = null;
                        
                        if (parameters.Length == 2) // Signature: OnChange(T oldValue, T newValue)
                        {
                            var actionType = typeof(Action<,>).MakeGenericType(property.ValueType, property.ValueType);
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { actionType, typeof(bool) });

                            if (subscribeMethod != null)
                            {
                                var handlerDelegate = Delegate.CreateDelegate(actionType, component, method);
                                subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate, true });
                            }
                        }
                        else if (parameters.Length == 1) // Signature: OnChange(T newValue)
                        {
                            var actionType = typeof(Action<>).MakeGenericType(property.ValueType);
                            var subscribeMethod = property.GetType().GetMethod("Subscribe", new[] { actionType, typeof(bool) });

                            if (subscribeMethod != null)
                            {
                                var handlerDelegate = Delegate.CreateDelegate(actionType, component, method);
                                subscription = (IDisposable)subscribeMethod.Invoke(property, new object[] { handlerDelegate, true });
                            }
                        }
                        else // Signature: OnChange()
                        {
                            subscription = property.Subscribe(_ => method.Invoke(component, null), true);
                        }

                        if (subscription != null)
                        {
                            subscriptionManager.Add(subscription);
                        }
                        else
                        {
                            _manager.Logger.Error($"[FluxFramework] Could not find a suitable 'Subscribe' method on property '{attribute.PropertyKey}' for handler '{method.Name}'. Check for API changes in ReactiveProperty.", component);
                        }
                    }
                    catch (Exception ex)
                    {
                        _manager.Logger.Error($"[FluxFramework] Error attaching property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}. Check method signature.", component);
                    }
                });
            }
            catch (Exception ex)
            {
                _manager.Logger.Error($"[FluxFramework] Error subscribing property change handler '{method.Name}' to key '{attribute.PropertyKey}': {ex.Message}", component);
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

                if (eventType == null)
                {
                    if (parameters.Length == 1 && typeof(IFluxEvent).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        eventType = parameters[0].ParameterType;
                    }
                    else if (parameters.Length > 1)
                    {
                        _manager.Logger.Warning($"[FluxFramework] Cannot auto-register event handler '{method.Name}' on '{component.GetType().Name}'. Method has more than one parameter. Use [FluxEventHandler(typeof(MyEvent))] on a method with zero or one parameter.", component);
                        return;
                    }
                }

                if (eventType == null)
                {
                    _manager.Logger.Warning($"[FluxFramework] Could not determine event type for handler '{method.Name}' on '{component.GetType().Name}'. Ensure the method has one parameter that inherits from IFluxEvent, or specify the type in the attribute.", component);
                    return;
                }

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
                    _manager.Logger.Error("[FluxFramework] Critical Error: Could not find the required 'IEventBus.Subscribe<T>(Action<T>, int)' method via reflection.", component);
                    return;
                }

                var genericSubscribeMethod = subscribeMethodInfo.MakeGenericMethod(eventType);
                var delegateType = typeof(Action<>).MakeGenericType(eventType);
                var eventHandlerDelegate = Delegate.CreateDelegate(delegateType, component, method);
                int priority = attribute.Priority;
                
                genericSubscribeMethod.Invoke(_manager.EventBus, new object[] { eventHandlerDelegate, priority });
            }
            catch (Exception ex)
            {
                _manager.Logger.Error($"[FluxFramework] Error registering EventHandler '{method.Name}' on '{component.GetType().Name}': {ex.Message}. Please ensure the method signature matches the event type.", component);
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
        /// Clears all cached data and forces re-discovery. (Mainly for Editor use)
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