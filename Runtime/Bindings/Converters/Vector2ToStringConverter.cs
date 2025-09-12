using System;
using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.Binding.Converters
{
    /// <summary>
    /// A default converter that transforms a Vector2 value into a formatted string.
    /// </summary>
    public class Vector2ToStringConverter : IValueConverter<Vector2, string>
    {
        public string Convert(Vector2 value) => $"{value.x},{value.y}";

        public Vector2 ConvertBack(string value)
        {
            var parts = value.Split(',');
            return parts.Length == 2 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y)
                ? new Vector2(x, y)
                : Vector2.zero;
        }

        object IValueConverter.Convert(object value)
        {
            if (value is Vector2 v) return Convert(v);
            return value?.ToString() ?? "0,0";
        }

        object IValueConverter.ConvertBack(object value)
        {
            if (value is string s) return ConvertBack(s);
            return default(Vector2);
        }
    }
}