namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a UI component is validated
    /// </summary>
    public class ValidationEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Name of the component being validated
        /// </summary>
        public string ComponentName { get; }

        /// <summary>
        /// Validation result
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Validation error messages (if any)
        /// </summary>
        public string[] ErrorMessages { get; }

        /// <summary>
        /// Property that was validated
        /// </summary>
        public string PropertyKey { get; }

        public ValidationEvent(string componentName, bool isValid, string propertyKey, params string[] errorMessages)
            : base("FluxFramework.Validation")
        {
            ComponentName = componentName;
            IsValid = isValid;
            ErrorMessages = errorMessages ?? new string[0];
            PropertyKey = propertyKey;
        }
    }
}
