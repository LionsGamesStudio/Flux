using System;
using FluxFramework.Core;
using FluxFramework.Binding;

namespace FluxFramework.Binding.Converters
{
    /// <summary>
    /// A default converter that transforms a boolean value into a formatted string.
    /// </summary>
    public class BoolToStringConverter : IValueConverter<bool, string>
    {
        public string Convert(bool value)
        {
            return value ? "True" : "False";
        }

        public bool ConvertBack(string value)
        {
            return bool.TryParse(value, out var result) ? result : false;
        }

        object IValueConverter.Convert(object value)
        {
            if (value is bool b) return Convert(b);
            return value?.ToString() ?? "False";
        }

        object IValueConverter.ConvertBack(object value)
        {
            if (value is string s) return ConvertBack(s);
            return default(bool);
        }
    }
}