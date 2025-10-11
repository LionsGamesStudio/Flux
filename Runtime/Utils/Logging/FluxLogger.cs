using System;
using System.Collections.Generic;

namespace FluxFramework.Utils
{
    /// <summary>
    /// The default implementation of the IFluxLogger service.
    /// Supports log levels, categories, context objects, and custom output handlers.
    /// </summary>
    public class FluxLogger : IFluxLogger
    {
        private readonly List<IFluxLogHandler> _handlers = new List<IFluxLogHandler>();
        private readonly Dictionary<string, LogLevel> _categoryLevels = new Dictionary<string, LogLevel>();
        private readonly object _handlerLock = new object();

        public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;
    
        public void AddHandler(IFluxLogHandler handler)
        {
            lock (_handlerLock)
            {
                if (handler != null && !_handlers.Contains(handler))
                {
                    _handlers.Add(handler);
                }
            }
        }

        public void RemoveHandler(IFluxLogHandler handler)
        {
            lock (_handlerLock)
            {
                _handlers.Remove(handler);
            }
        }

        public void SetCategoryLevel(string category, LogLevel level)
        {
            _categoryLevels[category] = level;
        }

        public void Debug(string message, UnityEngine.Object context = null, string category = "")
        {
            if (ShouldLog(LogLevel.Debug, category))
                Log(LogLevel.Debug, message, context, category);
        }

        public void Info(string message, UnityEngine.Object context = null, string category = "")
        {
            if (ShouldLog(LogLevel.Info, category))
                Log(LogLevel.Info, message, context, category);
        }

        public void Warning(string message, UnityEngine.Object context = null, string category = "")
        {
            if (ShouldLog(LogLevel.Warning, category))
                Log(LogLevel.Warning, message, context, category);
        }

        public void Error(string message, UnityEngine.Object context = null, string category = "")
        {
            if (ShouldLog(LogLevel.Error, category))
                Log(LogLevel.Error, message, context, category);
        }

        public void Exception(Exception exception, string message = "", UnityEngine.Object context = null, string category = "")
        {
            if (ShouldLog(LogLevel.Error, category))
            {
                var fullMessage = string.IsNullOrEmpty(message) ? exception.ToString() : $"{message}\n{exception}";
                Log(LogLevel.Error, fullMessage, context, category);
            }
        }
        
        private void Log(LogLevel level, string message, UnityEngine.Object context, string category)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = string.IsNullOrEmpty(category) ? "[FluxFramework]" : $"[FluxFramework:{category}]";
            var formattedMessage = $"{prefix} {message}";

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage, context);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage, context);
                    break;
            }

            lock (_handlerLock)
            {
                foreach (var handler in _handlers)
                {
                    try
                    {
                        handler.Log(level, message, category, timestamp);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"[FluxFramework] Error in log handler: {e.GetType().Name}", context);
                    }
                }
            }
        }
        
        private bool ShouldLog(LogLevel level, string category)
        {
            return level >= GetEffectiveLevel(category);
        }

        private LogLevel GetEffectiveLevel(string category)
        {
            if (!string.IsNullOrEmpty(category) && _categoryLevels.TryGetValue(category, out var categoryLevel))
            {
                return categoryLevel;
            }
            return CurrentLogLevel;
        }
    }
}