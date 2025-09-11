namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for validation logic
    /// </summary>
    /// <typeparam name="T">Type of value to validate</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validates the given value
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(T value);
    }
}
