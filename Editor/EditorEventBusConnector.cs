using UnityEditor;
using FluxFramework.Core;

namespace FluxFramework.Editor
{
    /// <summary>
    /// This editor-only class ensures that any open EventBusMonitorWindow is correctly
    /// re-subscribed to the EventBus after a domain reload (e.g., when entering play mode
    /// or after a script compilation). This solves the issue of the monitor not receiving
    // events after the C# AppDomain is reset by Unity.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorEventBusConnector
    {
        // The static constructor is called by Unity automatically after a domain reload.
        static EditorEventBusConnector()
        {
            // We don't need to do anything complex. The EventBusMonitorWindow's own OnEnable
            // method already contains the logic to subscribe. When Unity reloads the domain,
            // it will disable and then re-enable all open editor windows, which will trigger
            // our existing subscription logic automatically.

            // The one thing we MUST do is ensure that the EventBus itself is "awake"
            // in the editor context so that the window has something to subscribe to.
            // Calling a simple method on it is enough to trigger its static constructor if it hasn't run.
            EventBus.Initialize();
        }
    }
}