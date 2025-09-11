using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FluxFramework.Threading
{
    /// <summary>
    /// Thread-safe task scheduler for background operations
    /// </summary>
    public class FluxTaskScheduler
    {
        private static FluxTaskScheduler _instance;
        private static readonly object _lock = new object();

        private readonly TaskScheduler _backgroundScheduler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Singleton instance of the task scheduler
        /// </summary>
        public static FluxTaskScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new FluxTaskScheduler();
                        }
                    }
                }
                return _instance;
            }
        }

        private FluxTaskScheduler()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _backgroundScheduler = TaskScheduler.Default;
        }

        /// <summary>
        /// Schedules a task to run on a background thread
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <returns>Task representing the operation</returns>
        public Task ScheduleBackgroundTask(Action action)
        {
            return Task.Factory.StartNew(action, _cancellationTokenSource.Token, TaskCreationOptions.None, _backgroundScheduler);
        }

        /// <summary>
        /// Schedules a task to run on a background thread with a result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="function">Function to execute</param>
        /// <returns>Task representing the operation with result</returns>
        public Task<T> ScheduleBackgroundTask<T>(Func<T> function)
        {
            return Task.Factory.StartNew(function, _cancellationTokenSource.Token, TaskCreationOptions.None, _backgroundScheduler);
        }

        /// <summary>
        /// Schedules a task with a timeout
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if task completed within timeout</returns>
        public async Task<bool> ScheduleWithTimeout(Action action, int timeoutMs)
        {
            try
            {
                var task = ScheduleBackgroundTask(action);
                await Task.WhenAny(task, Task.Delay(timeoutMs, _cancellationTokenSource.Token));
                return task.IsCompleted;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FluxFramework] Task execution failed: {e}");
                return false;
            }
        }

        /// <summary>
        /// Cancels all scheduled tasks
        /// </summary>
        public void CancelAllTasks()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
