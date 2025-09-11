using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;
using FluxFramework.Validation;
using FluxFramework.Extensions;

namespace FluxFramework.Core
{
    /// <summary>
    /// Registry for automatic discovery and registration of classes marked with [FluxComponent].
    /// It handles the lifecycle and automatic setup of reactive properties and event handlers.
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
        /// Scans all assemblies for types marked with the [FluxComponent] attribute.
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
                    if (IsSystemAssembly(assembly.FullName))
                        continue;

                    var types = assembly.GetTypes()
                        .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract);

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

                            // This log can be noisy but is useful for debugging discovery issues.
                            // Debug.Log($"[FluxFramework] Discovered FluxComponent: {type.Name}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"[FluxFramework] Could not load types from assembly {assembly.FullName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FluxFramework] An error occurred while scanning assembly {assembly.FullName}: {ex.Message}");
                }
            }

            _isDiscovered = true;
            Debug.Log($"[FluxFramework] Discovered {_discoveredComponents.Count} FluxComponent types in {_componentsByCategory.Count} categories.");
        }

        /// <summary>
        /// Registers a specific instance of a MonoBehaviour with the framework.
        /// </summary>
        public static void RegisterComponentInstance(MonoBehaviour component)
        {
            if (component == null || _registeredInstances.Contains(component))
            {
                return; // Null or already registered.
            }

            var type = component.GetType();

            if (!_discoveredComponents.TryGetValue(type, out var attribute))
            {
                // This component might not have been discovered at startup. Check for the attribute now.
                attribute = type.GetCustomAttribute<FluxComponentAttribute>();
                if (attribute == null) return; // Not a FluxComponent.
                _discoveredComponents[type] = attribute;
            }

            if (!attribute.AutoRegister)
            {
                return; // This component is configured to be registered manually.
            }

            _registeredTypes.Add(type);
            _registeredInstances.Add(component);

            Debug.Log($"[FluxFramework] Registered FluxComponent instance: {type.Name} on GameObject '{component.gameObject.name}'.", component);

            CallRegistrationMethods(component, attribute);

            RegisterReactiveProperties(component);
            RegisterEventHandlers(component);

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
                    if (method.GetParameters().Length == 0)
                    {
                        method.Invoke(component, null);
                    }
                    else
                    {
                        Debug.LogWarning($"[FluxFramework] Registration method {method.Name} on {component.GetType().Name} has parameters and cannot be auto-called.", component);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Error calling registration method {method.Name} on {component.GetType().Name}: {ex.Message}", component);
                }
            }
        }

        /// <summary>
        /// Scans the component for fields marked with [ReactiveProperty] and registers them.
        /// </summary>
        private static void RegisterReactiveProperties(MonoBehaviour component)
        {
            var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    RegisterReactiveField(component, field, reactiveAttr);
                }
            }
        }

        /// <summary>
        /// Registers a single field as a reactive property.
        /// It detects validation attributes and creates a ValidatedReactiveProperty if necessary.
        /// </summary>
        private static void RegisterReactiveField(MonoBehaviour component, FieldInfo field, ReactivePropertyAttribute attribute)
        {
            var propertyKey = attribute.Key;

            try
            {
                var validators = GetValidatorsForField(field);

                IReactiveProperty property;
                object fieldValue = field.GetValue(component);

                if (validators.Count > 0)
                {
                    var genericValidatorType = typeof(IValidator<>).MakeGenericType(field.FieldType);
                    var validatorList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericValidatorType));

                    foreach (var validator in validators)
                    {
                        if (genericValidatorType.IsInstanceOfType(validator))
                        {
                            validatorList.Add(validator);
                        }
                    }

                    var propertyType = typeof(ValidatedReactiveProperty<>).MakeGenericType(field.FieldType);
                    property = (IReactiveProperty)Activator.CreateInstance(propertyType, fieldValue, validatorList);
                }
                else
                {
                    var propertyType = typeof(ReactiveProperty<>).MakeGenericType(field.FieldType);
                    property = (IReactiveProperty)Activator.CreateInstance(propertyType, fieldValue);
                }

                FluxManager.Instance.RegisterProperty(propertyKey, property);

                property.Subscribe(newValue => field.SetValue(component, newValue));

                if (attribute.Persistent)
                {
                    FluxPersistenceManager.RegisterPersistentProperty(propertyKey, property);
                }

                Debug.Log($"[FluxFramework] Registered {(validators.Count > 0 ? "Validated" : "")}ReactiveProperty '{propertyKey}' for {component.GetType().Name}.{field.Name}", component);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering ReactiveProperty '{propertyKey}' on '{component.name}': {ex.Message}", component);
            }
        }

        /// <summary>
        /// A helper method that inspects a field for validation attributes and returns a list of IValidator instances.
        /// </summary>
        private static List<object> GetValidatorsForField(FieldInfo field)
        {
            var validators = new List<object>();
            var fieldType = field.FieldType;

            var rangeAttr = field.GetCustomAttribute<FluxRangeAttribute>();
            if (rangeAttr != null)
            {
                if (typeof(IComparable).IsAssignableFrom(fieldType))
                {
                    var validatorType = typeof(RangeValidator<>).MakeGenericType(fieldType);
                    var min = Convert.ChangeType(rangeAttr.Min, fieldType);
                    var max = Convert.ChangeType(rangeAttr.Max, fieldType);
                    validators.Add(Activator.CreateInstance(validatorType, min, max));
                }
            }

            var stringLengthAttr = field.GetCustomAttribute<FluxStringLengthAttribute>();
            if (stringLengthAttr != null && fieldType == typeof(string))
            {
                validators.Add(new StringLengthValidator(stringLengthAttr));
            }

            var customValidationAttr = field.GetCustomAttribute<FluxValidationAttribute>();
            if (customValidationAttr?.ValidatorType != null)
            {
                try
                {
                    var validatorInterface = typeof(IValidator<>).MakeGenericType(fieldType);
                    if (validatorInterface.IsAssignableFrom(customValidationAttr.ValidatorType))
                    {
                        validators.Add(Activator.CreateInstance(customValidationAttr.ValidatorType));
                    }
                    else
                    {
                        Debug.LogWarning($"[FluxFramework] Custom validator '{customValidationAttr.ValidatorType.Name}' on field '{field.Name}' is not compatible with the field type '{fieldType.Name}'.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FluxFramework] Failed to create instance of custom validator '{customValidationAttr.ValidatorType.Name}': {ex.Message}");
                }
            }

            return validators;
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

                if (eventType == null && parameters.Length == 1 && typeof(IFluxEvent).IsAssignableFrom(parameters[0].ParameterType))
                {
                    eventType = parameters[0].ParameterType;
                }

                if (eventType == null)
                {
                    Debug.LogWarning($"[FluxFramework] Cannot determine event type for handler {component.GetType().Name}.{method.Name}", component);
                    return;
                }

                var subscribeMethod = typeof(EventBus).GetMethod("Subscribe", BindingFlags.Public | BindingFlags.Static);
                if (subscribeMethod != null)
                {
                    var genericSubscribeMethod = subscribeMethod.MakeGenericMethod(eventType);

                    var delegateType = typeof(Action<>).MakeGenericType(eventType);
                    var eventHandler = Delegate.CreateDelegate(delegateType, component, method);

                    int priority = attribute.Priority;
                    genericSubscribeMethod.Invoke(null, new object[] { eventHandler, priority });
                    Debug.Log($"[FluxFramework] Registered EventHandler for {eventType.Name} on {component.GetType().Name}.{method.Name} with priority {priority}", component);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering EventHandler {component.GetType().Name}.{method.Name}: {ex.Message}", component);
            }
        }

        /// <summary>
        /// Registers all MonoBehaviours in the scene that have the [FluxComponent] attribute.
        /// </summary>
        public static void RegisterAllComponentsInScene()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

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

        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") ||
                   assemblyName.StartsWith("Microsoft.") ||
                   assemblyName.StartsWith("Unity.") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName.StartsWith("UnityEditor") ||
                   assemblyName.StartsWith("mscorlib") ||
                   assemblyName.StartsWith("netstandard");
        }
        
        /// <summary>
        /// Clear all cached data and force reinitialization (Editor only)
        /// </summary>
        public static void ClearCache()
        {
            _discoveredComponents.Clear();
            _componentsByCategory.Clear();
            _registeredTypes.Clear();
            _registeredInstances.Clear();
            _isDiscovered = false;
            _isInitialized = false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only method to refresh component discovery during development
        /// </summary>
        [UnityEditor.MenuItem("Flux/Tools/Refresh Component Registry")]
        public static void EditorRefreshRegistry()
        {
            ClearCache();
            Initialize();
            Debug.Log("[FluxFramework] Component Registry refreshed from editor");
        }
#endif
    }
}