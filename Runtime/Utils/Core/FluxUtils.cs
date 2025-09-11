using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Utils
{
    /// <summary>
    /// Utility methods for common operations in the Flux Framework
    /// </summary>
    public static class FluxUtils
    {
        /// <summary>
        /// Safely executes an action and logs any exceptions
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="context">Context for error logging</param>
        public static void SafeExecute(Action action, string context = "")
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FluxFramework] Error in {context}: {e}");
            }
        }

        /// <summary>
        /// Safely executes a function and returns the result or default value
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">Function to execute</param>
        /// <param name="defaultValue">Default value if exception occurs</param>
        /// <param name="context">Context for error logging</param>
        /// <returns>Function result or default value</returns>
        public static T SafeExecute<T>(Func<T> func, T defaultValue = default, string context = "")
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FluxFramework] Error in {context}: {e}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Generates a unique identifier string
        /// </summary>
        /// <returns>Unique identifier</returns>
        public static string GenerateId()
        {
            return Guid.NewGuid().ToString("N")[..8]; // First 8 characters
        }

        /// <summary>
        /// Generates a unique identifier with prefix
        /// </summary>
        /// <param name="prefix">Prefix for the identifier</param>
        /// <returns>Unique identifier with prefix</returns>
        public static string GenerateId(string prefix)
        {
            return $"{prefix}_{GenerateId()}";
        }

        /// <summary>
        /// Checks if a string is a valid property key
        /// </summary>
        /// <param name="key">Key to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidPropertyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Must start with letter or underscore
            if (!char.IsLetter(key[0]) && key[0] != '_')
                return false;

            // Can only contain letters, numbers, dots, and underscores
            foreach (char c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Formats a property key for display
        /// </summary>
        /// <param name="key">Property key</param>
        /// <returns>Formatted display string</returns>
        public static string FormatPropertyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "Unknown";

            // Split by dots and capitalize each part
            var parts = key.Split('.');
            var formatted = new List<string>();

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                // Convert camelCase to Title Case
                var titleCase = System.Text.RegularExpressions.Regex.Replace(part, 
                    @"([a-z])([A-Z])", "$1 $2");
                
                formatted.Add(char.ToUpper(titleCase[0]) + titleCase[1..]);
            }

            return string.Join(" > ", formatted);
        }

        /// <summary>
        /// Deep copies an object using JSON serialization
        /// </summary>
        /// <typeparam name="T">Type to copy</typeparam>
        /// <param name="obj">Object to copy</param>
        /// <returns>Deep copy of the object</returns>
        public static T DeepCopy<T>(T obj) where T : class
        {
            if (obj == null)
                return null;

            try
            {
                var json = JsonUtility.ToJson(obj);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FluxFramework] Could not deep copy object: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Compares two objects for equality, handling null values
        /// </summary>
        /// <param name="obj1">First object</param>
        /// <param name="obj2">Second object</param>
        /// <returns>True if objects are equal</returns>
        public static bool SafeEquals(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;
            
            if (obj1 == null || obj2 == null)
                return false;

            return obj1.Equals(obj2);
        }

        /// <summary>
        /// Gets the hash code for an object, handling null values
        /// </summary>
        /// <param name="obj">Object to get hash code for</param>
        /// <returns>Hash code</returns>
        public static int SafeGetHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Converts a value to string with null handling
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="nullValue">String to return if value is null</param>
        /// <returns>String representation</returns>
        public static string SafeToString(object value, string nullValue = "null")
        {
            return value?.ToString() ?? nullValue;
        }

        /// <summary>
        /// Clamps a value between min and max
        /// </summary>
        /// <typeparam name="T">Comparable type</typeparam>
        /// <param name="value">Value to clamp</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Clamped value</returns>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }
    }
}
