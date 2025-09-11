using System;

namespace FluxFramework.Utils
{
    /// <summary>
    /// File-based log handler
    /// </summary>
    public class FileLogHandler : IFluxLogHandler
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public FileLogHandler(string filePath)
        {
            _filePath = filePath;
        }

        public void Log(LogLevel level, string message, string category, string timestamp)
        {
            try
            {
                lock (_lock)
                {
                    var logEntry = $"{timestamp} [{level}] {category}: {message}\n";
                    System.IO.File.AppendAllText(_filePath, logEntry);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[FluxFramework] Failed to write to log file: {e}");
            }
        }
    }
}
