namespace FluxFramework.Core
{
    /// <summary>
    /// Result of a validation operation
    /// </summary>
    public struct ValidationResult
    {
        public bool IsValid { get; }
        public string[] ErrorMessages { get; }

        public ValidationResult(bool isValid, params string[] errorMessages)
        {
            IsValid = isValid;
            ErrorMessages = errorMessages ?? new string[0];
        }

        public static ValidationResult Success => new ValidationResult(true);
        public static ValidationResult Failure(params string[] errors) => new ValidationResult(false, errors);
    }
}
