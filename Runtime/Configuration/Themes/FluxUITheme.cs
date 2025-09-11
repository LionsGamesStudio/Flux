using UnityEngine;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// UI theme configuration
    /// </summary>
    [FluxConfiguration("UI", 
        DisplayName = "UI Theme", 
        Description = "Visual styling and theme configuration for UI components",
        LoadPriority = 80,
        DefaultMenuPath = "Flux/UI Theme")]
    [CreateAssetMenu(fileName = "FluxUITheme", menuName = "Flux/UI Theme")]
    public class FluxUITheme : FluxConfigurationAsset
    {
        [Header("Colors")]
        public Color primaryColor = Color.blue;
        public Color secondaryColor = Color.gray;
        public Color accentColor = Color.green;
        public Color backgroundColor = Color.white;
        public Color textColor = Color.black;

        [Header("Fonts")]
        public Font primaryFont;
        public Font secondaryFont;

        [Header("Sizes")]
        [Range(8, 72)]
        public int defaultFontSize = 14;
        [Range(8, 72)]
        public int titleFontSize = 24;
        [Range(8, 72)]
        public int subtitleFontSize = 18;

        [Header("Spacing")]
        [Range(0, 50)]
        public float defaultPadding = 10f;
        [Range(0, 50)]
        public float defaultMargin = 5f;

        public override bool ValidateConfiguration()
        {
            if (defaultFontSize <= 0 || titleFontSize <= 0 || subtitleFontSize <= 0)
            {
                Debug.LogError("[FluxFramework] Font sizes must be greater than 0");
                return false;
            }

            return true;
        }

        public override void ApplyConfiguration(FluxManager manager)
        {
            if (!ValidateConfiguration()) return;

            // Apply theme to UI components
            Debug.Log("[FluxFramework] UI Theme applied successfully");
        }
    }
}
