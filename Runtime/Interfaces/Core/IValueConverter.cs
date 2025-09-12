namespace FluxFramework.Core
{
    /// <summary>
    /// The non-generic base interface for all value converters.
    /// It defines the contract for converting values using the base 'object' type.
    /// </summary>
    public interface IValueConverter 
    {
        /// <summary>
        /// Converts a value from a source type to a target type.
        /// </summary>
        /// <param name="value">The source value.</param>
        /// <returns>The converted value as an object.</returns>
        object Convert(object value);

        /// <summary>
        /// Converts a value from the target type back to the source type.
        /// </summary>
        /// <param name="value">The target value.</param>
        /// <returns>The converted source value as an object.</returns>
        object ConvertBack(object value);
    }

    /// <summary>
    /// Interface for strongly-typed value converters used in bindings.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    public interface IValueConverter<TSource, TTarget> : IValueConverter
    {
        TTarget Convert(TSource value);
        TSource ConvertBack(TTarget value);
    }
}