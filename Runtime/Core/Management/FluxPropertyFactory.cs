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
    internal static class FluxPropertyFactory
    {
        /// <summary>
        /// Scans a target object for all fields marked with [ReactiveProperty] and registers them with the framework.
        /// </summary>
        /// <param name="owner">The object instance (MonoBehaviour or ScriptableObject) that owns the properties.</param>
        public static void RegisterPropertiesFor(object owner)
        {
            var fields = owner.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr == null) continue;

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
        }

        /// <summary>
        /// Handles PATTERN A: Registers a property for a field with a primitive type.
        /// </summary>
        private static void RegisterImplicitProperty(object owner, FieldInfo field, ReactivePropertyAttribute attribute)
        {
            var propertyKey = attribute.Key;
            var ownerContext = owner as UnityEngine.Object; // For logging
            try
            {
                object initialValue = field.GetValue(owner);
                var fieldType = field.FieldType;
                var validators = GetValidatorsForField(field);

                IReactiveProperty property;

                if (validators.Count > 0)
                {
                    var propertyType = typeof(ValidatedReactiveProperty<>).MakeGenericType(fieldType);
                    var genericValidatorInterfaceType = typeof(IValidator<>).MakeGenericType(fieldType);
                    var validatorList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericValidatorInterfaceType));
                    foreach (var v in validators)
                    {
                        if (genericValidatorInterfaceType.IsInstanceOfType(v)) validatorList.Add(v);
                    }
                    property = (IReactiveProperty)Activator.CreateInstance(propertyType, initialValue, validatorList);
                }
                else
                {
                    var propertyType = typeof(ReactiveProperty<>).MakeGenericType(fieldType);
                    property = (IReactiveProperty)Activator.CreateInstance(propertyType, initialValue);
                }

                property.Subscribe(newValue => field.SetValue(owner, newValue));

                FluxManager.Instance.RegisterProperty(propertyKey, property, attribute.Persistent);
                if (attribute.Persistent) FluxPersistenceManager.RegisterPersistentProperty(propertyKey, property);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering implicit ReactiveProperty '{propertyKey}' on '{ownerContext?.name}': {ex.Message}", ownerContext);
            }
        }

        /// <summary>
        /// Handles PATTERN B: Configures and registers a field that is explicitly of type ReactiveProperty<T>.
        /// </summary>
        private static void RegisterAndConfigureExplicitProperty(object owner, FieldInfo field, ReactivePropertyAttribute attribute)
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
                FluxManager.Instance.RegisterProperty(propertyKey, finalProperty, attribute.Persistent);
                if (attribute.Persistent) FluxPersistenceManager.RegisterPersistentProperty(propertyKey, finalProperty);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FluxFramework] Error registering explicit ReactiveProperty '{propertyKey}' on '{ownerContext?.name}': {ex.Message}", ownerContext);
            }
        }

        /// <summary>
        /// A unified helper to get validators for a field.
        /// </summary>
        private static List<IValidator> GetValidatorsForField(FieldInfo field)
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