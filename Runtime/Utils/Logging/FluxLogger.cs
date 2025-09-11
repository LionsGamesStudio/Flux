using System;
using System.Collections.Generic;

namespace FluxFramework.Utils
{
    /// <summary>
    /// Logger utility for the Flux Framework with different log levels
    /// </summary>
    public static class FluxLogger
    {
        private static LogLevel _currentLogLevel = LogLevel.Info;
        private static readonly List<IFluxLogHandler> _handlers = new List<IFluxLogHandler>();
        private static readonly Dictionary<string, LogLevel> _categoryLevels = new Dictionary<string, LogLevel>();

        /// <summary>
        /// Gets or sets the current log level
        /// </summary>
        public static LogLevel CurrentLogLevel
        {
            get => _currentLogLevel;
            set => _currentLogLevel = value;
        }

        /// <summary>
        /// Adds a custom log handler
        /// </summary>
        /// <param name="handler">Log handler to add</param>
        public static void AddHandler(IFluxLogHandler handler)
        {
            if (handler != null && !_handlers.Contains(handler))
            {
                _handlers.Add(handler);
            }
        }

        /// <summary>
        /// Removes a custom log handler
        /// </summary>
        /// <param name="handler">Log handler to remove</param>
        public static void RemoveHandler(IFluxLogHandler handler)
        {
            _handlers.Remove(handler);
        }

        /// <summary>
        /// Sets the log level for a specific category
        /// </summary>
        /// <param name="category">Category name</param>
        /// <param name="level">Log level for this category</param>
        public static void SetCategoryLevel(string category, LogLevel level)
        {
            _categoryLevels[category] = level;
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Optional category</param>
        public static void Debug(string message, string category = "")
        {
            Log(LogLevel.Debug, message, category);
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Optional category</param>
        public static void Info(string message, string category = "")
        {
            Log(LogLevel.Info, message, category);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Optional category</param>
        public static void Warning(string message, string category = "")
        {
            Log(LogLevel.Warning, message, category);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="category">Optional category</param>
        public static void Error(string message, string category = "")
        {
            Log(LogLevel.Error, message, category);
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="exception">Exception to log</param>
        /// <param name="message">Additional message</param>
        /// <param name="category">Optional category</param>
        public static void Exception(Exception exception, string message = "", string category = "")
        {
            var fullMessage = string.IsNullOrEmpty(message) ? 
                exception.ToString() : 
                $"{message}\n{exception}";
            
            Log(LogLevel.Error, fullMessage, category);
        }

        private static void Log(LogLevel level, string message, string category)
        {
            // Check if we should log this level
            var effectiveLevel = GetEffectiveLevel(category);
            if (level < effectiveLevel)
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = string.IsNullOrEmpty(category) ? "[FluxFramework]" : $"[FluxFramework:{category}]";
            var formattedMessage = $"{timestamp} {prefix} {message}";

            // Log to Unity console
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
            }

            // Log to custom handlers
            foreach (var handler in _handlers)
            {
                try
                {
                    handler.Log(level, message, category, timestamp);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[FluxFramework] Error in log handler: {e}");
                }
            }
        }

        private static LogLevel GetEffectiveLevel(string category)
        {
            if (!string.IsNullOrEmpty(category) && _categoryLevels.TryGetValue(category, out var categoryLevel))
            {
                return categoryLevel;
            }
            return _currentLogLevel;
        }

        /// <summary>
        /// Clears all custom handlers
        /// </summary>
        public static void ClearHandlers()
        {
            _handlers.Clear();
        }

        /// <summary>
        /// Clears all category-specific log levels
        /// </summary>
        public static void ClearCategoryLevels()
        {
            _categoryLevels.Clear();
        }
    }
}
