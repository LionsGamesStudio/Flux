namespace FluxFramework.Threading
{
    /// <summary>
    /// Thread-safe statistics collector
    /// </summary>
    public class ThreadSafeStatistics
    {
        private readonly ThreadSafeCounter _eventCount = new ThreadSafeCounter();
        private readonly ThreadSafeCounter _errorCount = new ThreadSafeCounter();
        private readonly ThreadSafeCounter _bindingCount = new ThreadSafeCounter();
        private readonly ThreadSafeCounter _propertyCount = new ThreadSafeCounter();

        /// <summary>
        /// Total number of events processed
        /// </summary>
        public long EventCount => _eventCount.Value;

        /// <summary>
        /// Total number of errors encountered
        /// </summary>
        public long ErrorCount => _errorCount.Value;

        /// <summary>
        /// Total number of bindings created
        /// </summary>
        public long BindingCount => _bindingCount.Value;

        /// <summary>
        /// Total number of properties registered
        /// </summary>
        public long PropertyCount => _propertyCount.Value;

        /// <summary>
        /// Records an event
        /// </summary>
        public void RecordEvent() => _eventCount.Increment();

        /// <summary>
        /// Records an error
        /// </summary>
        public void RecordError() => _errorCount.Increment();

        /// <summary>
        /// Records a binding creation
        /// </summary>
        public void RecordBinding() => _bindingCount.Increment();

        /// <summary>
        /// Records a property registration
        /// </summary>
        public void RecordProperty() => _propertyCount.Increment();

        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void Reset()
        {
            _eventCount.Reset();
            _errorCount.Reset();
            _bindingCount.Reset();
            _propertyCount.Reset();
        }

        /// <summary>
        /// Gets a summary of all statistics
        /// </summary>
        /// <returns>Statistics summary string</returns>
        public override string ToString()
        {
            return $"Events: {EventCount}, Errors: {ErrorCount}, Bindings: {BindingCount}, Properties: {PropertyCount}";
        }
    }
}
