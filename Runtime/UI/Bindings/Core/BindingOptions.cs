using System;
using FluxFramework.Core;
using FluxFramework.Attributes;

namespace FluxFramework.Binding
{
    /// <summary>
    /// A container for options used to configure a UI binding.
    /// </summary>
    public class BindingOptions
    {
        public BindingMode Mode { get; set; } = BindingMode.OneWay;
        public IValueConverter Converter { get; set; } = null;
        public Type ConverterType { get; set; } = null; // Store the type as well
        public int UpdateDelayMs { get; set; } = 0;
        public bool ImmediateUpdate { get; set; } = true; // Kept for consistency

        public static BindingOptions Default { get; } = new BindingOptions();
    }
}