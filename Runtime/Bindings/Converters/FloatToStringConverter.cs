using System;
using FluxFramework.Core;

namespace FluxFramework.Binding.Converters
{
    /// <summary>
    /// A default converter that transforms a float value into a formatted string.
    /// </summary>
    public class FloatToStringConverter : IValueConverter<float, string>
    {
        public string Convert(float value)
        {
            return value.ToString("F0"); // "F0" = 0 decimal places
        }

        public float ConvertBack(string value)
        {
            return float.TryParse(value, out var result) ? result : 0f;
        }

        object IValueConverter.Convert(object value)
        {
            if (value is float f) return Convert(f);
            return value?.ToString() ?? "";
        }

        object IValueConverter.ConvertBack(object value)
        {
            if (value is string s) return ConvertBack(s);
            return default(float);
        }
    }
}