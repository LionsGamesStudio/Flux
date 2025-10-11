using System;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;
using FluxFramework.Validation;
using FluxFramework.Extensions;
using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// A centralized, internal factory responsible for discovering, creating, configuring,
    /// and registering all reactive properties for any given object.
    /// This ensures that the property creation logic is consistent across both MonoBehaviours and ScriptableObjects.
    /// </summary>
    public class FluxPropertyFactory : IFluxPropertyFactory
    {
        private static readonly Dictionary<Type, Type> _collectionWrapperMap = new Dictionary<Type, Type>
        {
            // Key = generic .NET collection type definition
            // Value = generic type definition of the corresponding reactive wrapper
            { typeof(List<>), typeof(ReactiveCollection<>) },
            { typeof(Dictionary<,>), typeof(ReactiveDictionary<,>) }
        };

        private static readonly Dictionary<Type, Type> _validatedCollectionWrapperMap = new Dictionary<Type, Type>
        {
            { typeof(List<>), typeof(ValidatedReactiveCollection<>) }
            
        };


        private readonly IFluxPropertyManager _propertyManager;
        private readonly IFluxPersistenceManager _persistenceManager;

        public FluxPropertyFactory(IFluxPropertyManager propertyManager, IFluxPersistenceManager persistenceManager)
        {
            _propertyManager = propertyManager ?? throw new ArgumentNullException(nameof(propertyManager));
            _persistenceManager = persistenceManager ?? throw new ArgumentNullException(nameof(persistenceManager));
        }

        /// <summary>
        /// Scans a target object for all fields marked with [ReactiveProperty] and registers them with the framework.
        /// </summary>
        /// <param name="owner">The object instance (MonoBehaviour or ScriptableObject) that owns the properties.</param>
        /// <returns>A collection of property names that were registered.</returns>
        public IEnumerable<string> RegisterPropertiesFor(object owner)
        {
            var fields = owner.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var registeredProperties = new List<string>();

            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr == null) continue;

                registeredProperties.Add(reactiveAttr.Key);

                // --- DISPATCHER LOGIC ---
                if (typeof(IReactiveProperty).IsAssignableFrom(field.FieldType))
                {
                    // PATTERN B: The field is a ReactiveProperty<T> itself.
                    RegisterAndConfigureExplicitProperty(owner, field, reactiveAttr);
                }
                else
                {
                    // PATTERN A: The field is a primitive type (int, float, etc.).
                    RegisterImplicitProperty(owner, field, reactiveAttr);
                }
            }

            return registeredProperties;
        }

        /// <summary>
        /// Handles PATTERN A: Registers a property for a field with a primitive type.
        /// </summary>
        private void RegisterImplicitProperty(object owner, FieldInfo field, ReactivePropertyAttribute attribute)
        {
            var propertyKey = attribute.Key;
            var ownerContext = owner as UnityEngine.Object; // For logging
            try
            {
                object initialValue = field.GetValue(owner);
                var fieldType = field.FieldType;
                var validators = GetValidatorsForField(field);

                IReactiveProperty property;
                Type propertyWrapperType = null;

                // --- Step 1: Determine the appropriate ReactiveProperty<T> type ---
                if (fieldType.IsGenericType)
                {
                    var genericDefinition = fieldType.GetGenericTypeDefinition();
                    var wrapperMap = validators.Count > 0 ? _validatedCollectionWrapperMap : _collectionWrapperMap;

                    if (wrapperMap.TryGetValue(genericDefinition, out var wrapperDefinition))
                    {
                        // We found a special collection type that has a dedicated reactive wrapper
                        propertyWrapperType = wrapperDefinition.MakeGenericType(fieldType.GetGenericArguments());
                    }
                }

                // If it's not a special collection, fall back to the default behavior
                if (propertyWrapperType == null)
                {
                    propertyWrapperType = validators.Count > 0
                        ? typeof(ValidatedReactiveProperty<>).MakeGenericType(fieldType)
                        : typeof(ReactiveProperty<>).MakeGenericType(fieldType);
                }

                // --- Step 2: Create the ReactiveProperty<T> instance ---
                if (validators.Count > 0)
                {
                    // Create an instance with validators
                    var genericValidatorInterfaceType = typeof(IValidator<>).MakeGenericType(fieldType);
                    var validatorList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericValidatorInterfaceType));
                    foreach (var v in validators)
                    {
                        if (genericValidatorInterfaceType.IsInstanceOfType(v)) validatorList.Add(v);
                    }
                    property = (IReactiveProperty)Activator.CreateInstance(propertyWrapperType, initialValue, validatorList);
                }
                else
                {
                    // Create a standard instance without validators
                    property = (IReactiveProperty)Activator.CreateInstance(propertyWrapperType, initialValue);
                }

                // --- Step 3: Finalize registration ---

                // 1. Basic sync: if the entire property reference changes, update the local field.
                property.Subscribe(newValue => field.SetValue(owner, newValue));

                // 2. Granular sync: Check if the property is syncable and delegate the setup.
                if (owner is MonoBehaviour monoBehaviour && property is IImplicitSyncable syncable)
                {
                    // The factory no longer needs to know HOW to sync. It just tells the property TO sync.
                    syncable.SetupImplicitSync(monoBehaviour, initialValue);
                }

                _propertyManager.RegisterProperty(propertyKey, property, attribute.Persistent);
                if (attribute.Persistent) _persistenceManager.RegisterPersistentProperty(propertyKey, property);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering implicit ReactiveProperty '{propertyKey}' on '{ownerContext?.name}': {ex.Message}", ownerContext);
            }
        }

        /// <summary>
        /// Handles PATTERN B: Configures and registers a field that is explicitly of type ReactiveProperty<T>.
        /// </summary>
        private void RegisterAndConfigureExplicitProperty(object owner, FieldInfo field, ReactivePropertyAttribute attribute)
        {
            var propertyKey = attribute.Key;
            var ownerContext = owner as UnityEngine.Object; // For logging
            try
            {
                var initialProperty = (IReactiveProperty)field.GetValue(owner);
                if (initialProperty == null)
                {
                    var fieldGenericType = field.FieldType.GetGenericArguments()[0];
                    initialProperty = (IReactiveProperty)Activator.CreateInstance(typeof(ReactiveProperty<>).MakeGenericType(fieldGenericType));
                    Debug.LogWarning($"[FluxFramework] ReactiveProperty '{field.Name}' on '{ownerContext?.name}' was not initialized. Created with default value.", ownerContext);
                }
                object initialValue = initialProperty.GetValue();
                var valueType = initialProperty.ValueType;
                var validators = GetValidatorsForField(field);

                IReactiveProperty finalProperty;
                if (validators.Count > 0)
                {
                    var propertyType = typeof(ValidatedReactiveProperty<>).MakeGenericType(valueType);
                    var genericValidatorInterfaceType = typeof(IValidator<>).MakeGenericType(valueType);
                    var validatorList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericValidatorInterfaceType));
                    foreach (var v in validators)
                    {
                        if (genericValidatorInterfaceType.IsInstanceOfType(v)) validatorList.Add(v);
                    }
                    finalProperty = (IReactiveProperty)Activator.CreateInstance(propertyType, initialValue, validatorList);
                }
                else
                {
                    finalProperty = initialProperty;
                }

                field.SetValue(owner, finalProperty);
                _propertyManager.RegisterProperty(propertyKey, finalProperty, attribute.Persistent);
                if (attribute.Persistent) _persistenceManager.RegisterPersistentProperty(propertyKey, finalProperty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering explicit ReactiveProperty '{propertyKey}' on '{ownerContext?.name}': {ex.Message}", ownerContext);
            }
        }

        /// <summary>
        /// A unified helper to get validators for a field.
        /// </summary>
        private List<IValidator> GetValidatorsForField(FieldInfo field)
        {
            var validators = new List<IValidator>();
            var validationAttributes = field.GetCustomAttributes<FluxValidationAttribute>(true);
            foreach (var attr in validationAttributes)
            {
                var validator = attr.CreateValidator(field);
                if (validator != null) validators.Add(validator);
            }
            return validators;
        }
    }
}