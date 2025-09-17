using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.VisualScripting.Editor
{
    [CreateAssetMenu(fileName = "FluxGraphTheme", menuName = "Flux/Visual Scripting/Editor Theme")]
    public class FluxGraphTheme : ScriptableObject
    {
        [Serializable]
        public struct CategoryColor
        {
            public string CategoryName;
            public Color HeaderColor;
        }

        public Color DefaultHeaderColor = new Color(0.2f, 0.2f, 0.2f);
        public List<CategoryColor> CategoryColors = new List<CategoryColor>();

        private Dictionary<string, Color> _colorMap;

        /// <summary>
        /// Gets the color for a specific category. If not found, returns the default color.
        /// </summary>
        public Color GetColorForCategory(string category)
        {
            if (_colorMap == null)
            {
                _colorMap = new Dictionary<string, Color>();
                foreach (var catColor in CategoryColors)
                {
                    _colorMap[catColor.CategoryName] = catColor.HeaderColor;
                }
            }

            if (!string.IsNullOrEmpty(category) && _colorMap.TryGetValue(category, out var color))
            {
                return color;
            }
            return DefaultHeaderColor;
        }
    }
}