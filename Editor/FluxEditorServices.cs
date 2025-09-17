using UnityEditor;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Configuration;

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
        public IValueConverterRegistry ValueConverterRegistry => null;
        public IFluxConfigurationManager ConfigurationManager => null;
        public IFluxPersistenceManager PersistenceManager => null;
        public IFluxPropertyFactory PropertyFactory => null;
        public IFluxThreadManager Threading => new EditorThreadManager();

        public UnityEngine.Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null;
        public void StopCoroutine(UnityEngine.Coroutine routine) { }
    }
}