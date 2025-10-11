using System;
using UnityEngine;

namespace FluxFramework.Utils
{
    /// <summary>
    /// Defines the contract for the framework's logging service.
    /// </summary>
    public interface IFluxLogger
    {
        /// <summary>
        /// Gets or sets the minimum log level to be processed.
        /// </summary>
        LogLevel CurrentLogLevel { get; set; }
        
        /// <summary>
        /// Adds a custom output handler for log messages.
        /// </summary>
        /// <param name="handler">The log handler to add.</param>
        void AddHandler(IFluxLogHandler handler);

        /// <summary>
        /// Removes a custom log handler.
        /// </summary>
        /// <param name="handler">The log handler to remove.</param>
        void RemoveHandler(IFluxLogHandler handler);
        
        /// <summary>
        /// Sets a specific log level for a given category, overriding the global level.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="level">The log level for this category.</param>
        void SetCategoryLevel(string category, LogLevel level);
        
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="context">Optional Unity Object to provide context.</param>
        /// <param name="category">Optional category for the message.</param>
        void Debug(string message, UnityEngine.Object context = null, string category = "");

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void Info(string message, UnityEngine.Object context = null, string category = "");
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void Warning(string message, UnityEngine.Object context = null, string category = "");

        /// <summary>
        /// Logs an error message.
        /// </summary>
        void Error(string message, UnityEngine.Object context = null, string category = "");

        /// <summary>
        /// Logs an exception.
        /// </summary>
        void Exception(Exception exception, string message = "", UnityEngine.Object context = null, string category = "");
    }
}