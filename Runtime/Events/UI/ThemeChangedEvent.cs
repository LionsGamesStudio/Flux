namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when the theme changes
    /// </summary>
    public class ThemeChangedEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Name of the new theme
        /// </summary>
        public string ThemeName { get; }

        /// <summary>
        /// Previous theme name
        /// </summary>
        public string PreviousTheme { get; }

        public ThemeChangedEvent(string themeName, string previousTheme = null)
            : base("FluxFramework.UI.ThemeManager")
        {
            ThemeName = themeName;
            PreviousTheme = previousTheme;
        }
    }
}
