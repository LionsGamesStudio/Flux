using System;
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
        public int UpdateDelayMs { get; set; } = 0;
        public bool ImmediateUpdate { get; set; } = false;

        public static BindingOptions Default { get; } = new BindingOptions();
    }

    // A non-generic interface for IValueConverter to store it in BindingOptions
    public interface IValueConverter
    {
        object Convert(object value);
        object ConvertBack(object value);
    }
}