using System.Threading;

namespace FluxFramework.Threading
{
    /// <summary>
    /// Thread-safe counter for performance monitoring
    /// </summary>
    public class ThreadSafeCounter
    {
        private long _value;

        /// <summary>
        /// Current counter value
        /// </summary>
        public long Value => Interlocked.Read(ref _value);

        /// <summary>
        /// Increments the counter and returns the new value
        /// </summary>
        /// <returns>New counter value</returns>
        public long Increment()
        {
            return Interlocked.Increment(ref _value);
        }

        /// <summary>
        /// Decrements the counter and returns the new value
        /// </summary>
        /// <returns>New counter value</returns>
        public long Decrement()
        {
            return Interlocked.Decrement(ref _value);
        }

        /// <summary>
        /// Adds a value to the counter and returns the new value
        /// </summary>
        /// <param name="value">Value to add</param>
        /// <returns>New counter value</returns>
        public long Add(long value)
        {
            return Interlocked.Add(ref _value, value);
        }

        /// <summary>
        /// Sets the counter to a specific value
        /// </summary>
        /// <param name="value">New value</param>
        /// <returns>Previous value</returns>
        public long Set(long value)
        {
            return Interlocked.Exchange(ref _value, value);
        }

        /// <summary>
        /// Resets the counter to zero
        /// </summary>
        /// <returns>Previous value</returns>
        public long Reset()
        {
            return Interlocked.Exchange(ref _value, 0);
        }

        /// <summary>
        /// Compares and exchanges the counter value atomically
        /// </summary>
        /// <param name="value">New value</param>
        /// <param name="comparand">Expected current value</param>
        /// <returns>Original value</returns>
        public long CompareExchange(long value, long comparand)
        {
            return Interlocked.CompareExchange(ref _value, value, comparand);
        }
    }
}
