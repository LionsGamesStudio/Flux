using System;

namespace FluxFramework.Binding
{
    public interface IValueConverterRegistry
    {
        void Initialize();
        Type FindConverterType(Type sourceType, Type targetType);
    }
}