using UnityEngine;
using FluxFramework.Configuration;

namespace FluxFramework.UI
{
    /// <summary>
    /// A static manager that holds the currently active UI theme for the application.
    /// Components can query this manager to style themselves consistently.
    /// </summary>
    public static class UIThemeManager
    {
        /// <summary>
        /// The currently active UI theme asset.
        /// </summary>
        public static FluxUITheme CurrentTheme { get; private set; }

        /// <summary>
        /// Sets the active theme for the application.
        /// This is typically called by the FluxUITheme's ApplyConfiguration method.
        /// </summary>
        public static void SetTheme(FluxUITheme theme)
        {
            CurrentTheme = theme;
            Debug.Log($"[FluxFramework] UI Theme '{theme.name}' has been applied.", theme);
        }
    }
}