using System;
using System.Collections.Generic;
using System.Linq;
using FluxFramework.Core;

namespace FluxFramework.Binding
{
    /// <summary>
    /// A registry that discovers and provides IValueConverter types.
    /// This allows the binding system to automatically find a suitable converter for type mismatches.
    /// </summary>
    public class ValueConverterRegistry : IValueConverterRegistry
    {
        private readonly Dictionary<Tuple<Type, Type>, Type> _converters = new Dictionary<Tuple<Type, Type>, Type>();
        private bool _isInitialized = false;
        
        public ValueConverterRegistry() { }

        public void Initialize()
        {
            if (_isInitialized) return;
            _converters.Clear();

            // At runtime, we must scan all loaded assemblies.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface || !typeof(IValueConverter).IsAssignableFrom(type)) continue;

                        var converterInterface = type.GetInterfaces().FirstOrDefault(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValueConverter<,>));

                        if (converterInterface != null)
                        {
                            var genericArgs = converterInterface.GetGenericArguments();
                            var sourceType = genericArgs[0];
                            var targetType = genericArgs[1];
                            var key = new Tuple<Type, Type>(sourceType, targetType);
                            _converters[key] = type;
                        }
                    }
                }
                catch { /* Silently ignore assemblies that fail to load types */ }
            }

            _isInitialized = true;
            FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] ValueConverterRegistry initialized. Found {_converters.Count} converters.");
        }

        public Type FindConverterType(Type sourceType, Type targetType)
        {
            if (!_isInitialized) Initialize();
            var key = new Tuple<Type, Type>(sourceType, targetType);
            _converters.TryGetValue(key, out var converterType);
            return converterType;
        }
    }
}