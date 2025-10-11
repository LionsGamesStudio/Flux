using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace FluxFramework.Core
{
    /// <summary>
    /// Manages thread-safe operations for the Flux Framework
    /// </summary>
    public class FluxThreadManager : IFluxThreadManager
    {
        private readonly ConcurrentQueue<Action> _mainThreadActions = new();
        private SynchronizationContext _mainThreadContext;
        private int _maxActionsPerFrame = 100;

        /// <summary>
        /// Initializes the thread manager
        /// </summary>
        public void Initialize()
        {
            _mainThreadContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Executes an action on the main thread in a thread-safe manner
        /// </summary>
        /// <param name="action">Action to execute on main thread</param>
        public void ExecuteOnMainThread(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                action?.Invoke();
            }
            else
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        /// <summary>
        /// Processes all queued main thread actions
        /// </summary>
        public void ProcessMainThreadActions()
        {
            for (int i = 0; i < _maxActionsPerFrame; ++i)
            {
                if (_mainThreadActions.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Flux.Manager.Logger.Exception(e, "Error executing main thread action");
                    }
                }
                else
                {
                    // No more actions to process
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the number of queued actions
        /// </summary>
        public int QueuedActionCount => _mainThreadActions.Count;

        /// <summary>
        /// Checks if the current thread is the main thread
        /// </summary>
        /// <returns>True if on main thread</returns>
        public bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == 1;
        }

        /// <summary>
        /// Clears all queued actions
        /// </summary>
        public void ClearQueue()
        {
            while (_mainThreadActions.TryDequeue(out _)) { }
        }

        public void SetMaxActionsPerFrame(int count)
        {
            _maxActionsPerFrame = Mathf.Max(1, count);
        }
    }
}
