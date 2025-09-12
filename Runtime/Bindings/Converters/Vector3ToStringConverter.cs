using System;
using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.Binding.Converters
{
    /// <summary>
    /// A default converter that transforms a Vector3 value into a formatted string.
    /// </summary>
    public class Vector3ToStringConverter : IValueConverter<Vector3, string>
    {
        public string Convert(Vector3 value) => $"{value.x},{value.y},{value.z}";

        public Vector3 ConvertBack(string value)
        {
            var parts = value.Split(',');
            return parts.Length == 3 &&
                    float.TryParse(parts[0], out var x) &&
                    float.TryParse(parts[1], out var y) &&
                    float.TryParse(parts[2], out var z)
                ? new Vector3(x, y, z)
                : Vector3.zero;
        }

        object IValueConverter.Convert(object value)
        {
            if (value is Vector3 v) return Convert(v);
            return value?.ToString() ?? "0,0,0";
        }

        object IValueConverter.ConvertBack(object value)
        {
            if (value is string s) return ConvertBack(s);
            return default(Vector3);
        }
    }
}