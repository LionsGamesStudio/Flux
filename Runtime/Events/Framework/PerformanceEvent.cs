namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when a performance metric is recorded
    /// </summary>
    public class PerformanceEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Name of the metric
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// Value of the metric
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// Unit of measurement
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Category of the metric
        /// </summary>
        public string Category { get; }

        public PerformanceEvent(string metricName, float value, string unit = "ms", string category = "General")
            : base("FluxFramework.Performance")
        {
            MetricName = metricName;
            Value = value;
            Unit = unit;
            Category = category;
        }
    }
}
