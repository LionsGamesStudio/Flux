namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for value converters used in bindings
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TTarget">Target type</typeparam>
    public interface IValueConverter<TSource, TTarget>
    {
        /// <summary>
        /// Converts from source to target type
        /// </summary>
        /// <param name="value">Source value</param>
        /// <returns>Converted target value</returns>
        TTarget Convert(TSource value);

        /// <summary>
        /// Converts from target back to source type
        /// </summary>
        /// <param name="value">Target value</param>
        /// <returns>Converted source value</returns>
        TSource ConvertBack(TTarget value);
    }
}
