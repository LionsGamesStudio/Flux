using System;
using FluxFramework.Core;

namespace FluxFramework.Editor
{
    /// <summary>
    /// A simple implementation of IFluxThreadManager for editor-only contexts.
    /// It assumes all actions are already on the main thread and executes them immediately.
    /// </summary>
    public class EditorThreadManager : IFluxThreadManager
    {
        public void ExecuteOnMainThread(Action action)
        {
            action?.Invoke();
        }

        public bool IsMainThread()
        {
            return true;
        }

        public void SetMaxActionsPerFrame(int maxActions)
        {
            // No-op in editor context
        }
    }
}