using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using FluxFramework.Core;

namespace FluxFramework.Threading
{
    /// <summary>
    /// Thread-safe utilities for the Flux Framework
    /// </summary>
    public static class ThreadSafeUtilities
    {
        private static volatile bool _isMainThread = true;

        /// <summary>
        /// Checks if the current thread is the main Unity thread
        /// </summary>
        /// <returns>True if on main thread</returns>
        public static bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == 1;
        }

        /// <summary>
        /// Executes an action on the main thread safely
        /// </summary>
        /// <param name="action">Action to execute</param>
        public static void ExecuteOnMainThread(Action action)
        {
            FluxManager.Instance.Threading.ExecuteOnMainThread(action);
        }

        /// <summary>
        /// Executes an action on the main thread and waits for completion
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if action completed within timeout</returns>
        public static bool ExecuteOnMainThreadAndWait(Action action, int timeoutMs = 5000)
        {
            if (IsMainThread())
            {
                action?.Invoke();
                return true;
            }

            var completed = false;
            var resetEvent = new ManualResetEventSlim(false);

            FluxManager.Instance.Threading.ExecuteOnMainThread(() =>
            {
                try
                {
                    action?.Invoke();
                }
                finally
                {
                    completed = true;
                    resetEvent.Set();
                }
            });

            return resetEvent.Wait(timeoutMs) && completed;
        }

        /// <summary>
        /// Executes a function on the main thread and returns the result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">Function to execute</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>Function result or default value if timeout</returns>
        public static T ExecuteOnMainThread<T>(Func<T> func, int timeoutMs = 5000)
        {
            if (IsMainThread())
            {
                return func != null ? func() : default(T);
            }

            var result = default(T);
            var resetEvent = new ManualResetEventSlim(false);

            FluxManager.Instance.Threading.ExecuteOnMainThread(() =>
            {
                try
                {
                    result = func != null ? func() : default(T);
                }
                finally
                {
                    resetEvent.Set();
                }
            });

            return resetEvent.Wait(timeoutMs) ? result : default(T);
        }
    }
}
