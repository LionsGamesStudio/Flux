using System;

namespace FluxFramework.Events
{
    /// <summary>
    /// Event raised when there's an error in the framework
    /// </summary>
    public class FrameworkErrorEvent : FluxFramework.Core.FluxEventBase
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Exception that caused the error (if any)
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Severity level of the error
        /// </summary>
        public ErrorSeverity Severity { get; }

        /// <summary>
        /// Component or system where the error occurred
        /// </summary>
        public string ErrorSource { get; }

        public FrameworkErrorEvent(string errorMessage, Exception exception = null, ErrorSeverity severity = ErrorSeverity.Error, string errorSource = null)
            : base(errorSource ?? "FluxFramework.Unknown")
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            Severity = severity;
            ErrorSource = errorSource ?? Source;
        }
    }
}
