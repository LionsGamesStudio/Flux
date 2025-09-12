namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for validation logic
    /// </summary>
    /// <typeparam name="T">Type of value to validate</typeparam>
    public interface IValidator<T> : IValidator
    {
        /// <summary>
        /// Validates the given value
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(T value);
    }

    /// <summary>
    /// A non-generic marker interface for all validators.
    /// Its purpose is to allow collections of different generic IValidator<T> instances
    /// (e.g., a List<IValidator> containing both an IValidator<int> and an IValidator<string>).
    /// </summary>
    public interface IValidator { }
}
