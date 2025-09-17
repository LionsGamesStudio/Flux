using UnityEditor;
using FluxFramework.Core;
using System;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A bridge that connects the runtime EventBus (from Play Mode) to the editor-only
    /// EventBus used by monitoring tools. It forwards all published events.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorEventBusConnector
    {
        private static IDisposable _runtimeSubscription;

        static EditorEventBusConnector()
        {
            // Subscribe to Unity's play mode state changes to know when to connect/disconnect.
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    // The runtime FluxManager is now available.
                    ConnectToRuntimeBus();
                    break;
                
                case PlayModeStateChange.ExitingPlayMode:
                    // The runtime FluxManager is about to be destroyed.
                    DisconnectFromRuntimeBus();
                    break;
            }
        }

        private static void ConnectToRuntimeBus()
        {
            // Ensure we don't subscribe twice.
            DisconnectFromRuntimeBus();

            if (Flux.Manager?.EventBus != null)
            {
                // Subscribe to the global event publisher on the RUNTIME bus.
                // For every event published in the game, we forward it to the EDITOR bus.
                Flux.Manager.EventBus.OnEventPublished += ForwardEventToEditorBus;
            }
        }

        private static void DisconnectFromRuntimeBus()
        {
            if (Flux.Manager?.EventBus != null)
            {
                // Unsubscribe to prevent errors when exiting play mode.
                Flux.Manager.EventBus.OnEventPublished -= ForwardEventToEditorBus;
            }
        }
        
        private static void ForwardEventToEditorBus(IFluxEvent runtimeEvent)
        {
            // Take the event from the runtime bus and publish it on the editor bus.
            // Any editor window (like EventBusMonitorWindow) listening to FluxEditorServices.EventBus
            // will now receive it.
            if (FluxEditorServices.EventBus != null && runtimeEvent != null)
            {
                FluxEditorServices.EventBus.Publish(runtimeEvent);
            }
        }
    }
}