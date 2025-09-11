namespace FluxFramework.Utils
{
    /// <summary>
    /// Interface for custom log handlers
    /// </summary>
    public interface IFluxLogHandler
    {
        void Log(LogLevel level, string message, string category, string timestamp);
    }
}
