using System;

namespace FluxFramework.Testing
{
    /// <summary>
    /// Represents the outcome of a single test execution.
    /// </summary>
    public class FluxTestResult
    {
        public string FixtureName { get; set; }
        public string TestName { get; set; }
        public TestStatus Status { get; set; }
        public string Message { get; set; } // Used for failure messages
        public long DurationMilliseconds { get; set; }
    }

    /// <summary>
    /// Defines the possible outcomes of a test.
    /// </summary>
    public enum TestStatus
    {
        NotRun,
        Success,
        Failed
    }
}