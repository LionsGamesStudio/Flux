namespace FluxFramework.Core
{
    /// <summary>
    /// Interface for performance monitoring
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// Records a metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value to record</param>
        /// <param name="unit">Unit of measurement</param>
        void RecordMetric(string metricName, float value, string unit = "ms");

        /// <summary>
        /// Gets a recorded metric value
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <returns>Latest recorded value or 0 if not found</returns>
        float GetMetric(string metricName);

        /// <summary>
        /// Clears all recorded metrics
        /// </summary>
        void ClearMetrics();
    }
}
