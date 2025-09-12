using System;
using FluxFramework.Core;

namespace FluxFramework.Binding.Converters
{
    /// <summary>
    /// A default converter that transforms an integer value into a formatted string.
    /// </summary>
    public class IntToStringConverter : IValueConverter<int, string>
    {
        public string Convert(int value)
        {
            return value.ToString();
        }

        public int ConvertBack(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        object IValueConverter.Convert(object value)
        {
            if (value is int i) return Convert(i);
            return value?.ToString() ?? "";
        }

        object IValueConverter.ConvertBack(object value)
        {
            if (value is string s) return ConvertBack(s);
            return default(int);
        }
    }
}