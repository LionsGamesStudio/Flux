namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a UI component is validated
    /// </summary>
    public class ValidationEvent : FluxFramework.Core.FluxEventBase
    {
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

        public ValidationEvent(bool isValid, string propertyKey, params string[] errorMessages)
            : base("FluxFramework.Validation")
        {
            IsValid = isValid;
            ErrorMessages = errorMessages ?? new string[0];
            PropertyKey = propertyKey;
        }
    }
}
