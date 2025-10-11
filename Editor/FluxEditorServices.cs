using System;
using System.Collections.Generic;
using UnityEditor;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Configuration;
using FluxFramework.Utils;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Provides stable, editor-only instances of Flux services (EventBus, etc.)
    /// for editor tools like monitors and inspectors. This ensures tools work
    /// correctly outside of play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class FluxEditorServices
    {
        public static IEventBus EventBus { get; private set; }
        public static IFluxComponentRegistry ComponentRegistry { get; private set; }

        static FluxEditorServices()
        {
            // Create a lightweight, editor-safe environment
            var threadManager = new EditorThreadManager();
            EventBus = new EventBus(threadManager);
            EventBus.Initialize();

            var editorManagerStub = new EditorFluxManagerStub();
            ComponentRegistry = new FluxComponentRegistry(editorManagerStub);
            ComponentRegistry.Initialize();
        }
    }

    /// <summary>
    /// A minimal, non-functional implementation of IFluxManager used to satisfy
    /// dependencies for editor services that don't need a full runtime manager.
    /// </summary>
    internal class EditorFluxManagerStub : IFluxManager
    {
        // On retourne 'null' pour les services dont le registry n'a pas besoin directement.
        // Si à l'avenir il en a besoin, nous pourrions instancier ici des versions "editor" de ces services.
        public bool IsInitialized => true;
        public IEventBus EventBus => null;
        public IFluxPropertyManager Properties => null;
        public IFluxComponentRegistry Registry => null;
        public IReactiveBindingSystem BindingSystem => null;
        public IBindingFactory BindingFactory => null;
        public IValueConverterRegistry ValueConverterRegistry => null;
        public IFluxConfigurationManager ConfigurationManager => null;
        public IFluxPersistenceManager PersistenceManager => null;
        public IFluxPropertyFactory PropertyFactory => null;
        public IFluxThreadManager Threading => new EditorThreadManager();
        public IFluxLogger Logger => new EditorLogger();

        public UnityEngine.Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(UnityEngine.Coroutine routine) { }
    }

    internal class EditorLogger : IFluxLogger
    {
        public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

        private readonly List<IFluxLogHandler> _handlers =
            new List<IFluxLogHandler>();

        private readonly Dictionary<string, LogLevel> _categoryLevels =
            new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);

        public void AddHandler(IFluxLogHandler handler)
        {
            if (handler == null) return;
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public void RemoveHandler(IFluxLogHandler handler)
        {
            if (handler == null) return;
            _handlers.Remove(handler);
        }

        public void SetCategoryLevel(string category, LogLevel level)
        {
            if (string.IsNullOrEmpty(category)) return;
            _categoryLevels[category] = level;
        }

        private bool ShouldLog(LogLevel level, string category)
        {
            var effective = CurrentLogLevel;
            if (!string.IsNullOrEmpty(category) && _categoryLevels.TryGetValue(category, out var catLevel))
                effective = catLevel;

            return System.Convert.ToInt32(level) >= System.Convert.ToInt32(effective);
        }

        private string FormatMessage(string category, string message)
        {
            return string.IsNullOrEmpty(category) ? message : $"[{category}] {message}";
        }

        private void NotifyHandlers(LogLevel level, string category, string message, UnityEngine.Object context, System.Exception exception = null)
        {
            foreach (var handler in _handlers)
            {
                try
                {
                    // Expected handler signature:
                    // void Log(LogLevel level, string message, string category, timestamp)
                    handler.Log(level, message, category, DateTime.Now.ToString("o"));
                }
                catch
                {
                    // Swallow handler exceptions to avoid breaking editor tooling.
                }
            }
        }

        public void Debug(string message, UnityEngine.Object context = null, string category = "")
        {
            if (!ShouldLog(LogLevel.Debug, category)) return;
            var formatted = FormatMessage(category, message);
            UnityEngine.Debug.Log(formatted, context);
            NotifyHandlers(LogLevel.Debug, category, message, context);
        }

        public void Info(string message, UnityEngine.Object context = null, string category = "")
        {
            if (!ShouldLog(LogLevel.Info, category)) return;
            var formatted = FormatMessage(category, message);
            UnityEngine.Debug.Log(formatted, context);
            NotifyHandlers(LogLevel.Info, category, message, context);
        }

        public void Warning(string message, UnityEngine.Object context = null, string category = "")
        {
            if (!ShouldLog(LogLevel.Warning, category)) return;
            var formatted = FormatMessage(category, message);
            UnityEngine.Debug.LogWarning(formatted, context);
            NotifyHandlers(LogLevel.Warning, category, message, context);
        }

        public void Error(string message, UnityEngine.Object context = null, string category = "")
        {
            if (!ShouldLog(LogLevel.Error, category)) return;
            var formatted = FormatMessage(category, message);
            UnityEngine.Debug.LogError(formatted, context);
            NotifyHandlers(LogLevel.Error, category, message, context);
        }

        public void Exception(System.Exception exception, string message = "", UnityEngine.Object context = null, string category = "")
        {
            if (!ShouldLog(LogLevel.Error, category)) return;
            var formatted = FormatMessage(category, message);
            if (exception != null)
                UnityEngine.Debug.LogException(exception, context);
            else
                UnityEngine.Debug.LogError(formatted, context);

            NotifyHandlers(LogLevel.Error, category, message, context, exception);
        }
    }
}