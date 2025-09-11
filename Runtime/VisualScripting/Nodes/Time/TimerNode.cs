using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that functions like a timer, triggering an output port every frame (tick)
    /// for a specified duration, and another port upon completion.
    /// </summary>
    [CreateAssetMenu(fileName = "TimerNode", menuName = "Flux/Visual Scripting/Time/Timer")]
    public class TimerNode : FluxNodeBase
    {
        [SerializeField] private float _duration = 1f;
        [SerializeField] private bool _loop = false;
        
        private readonly Dictionary<GameObject, Coroutine> _runningTimers = new Dictionary<GameObject, Coroutine>();

        public override string NodeName => "Timer";
        public override string Category => "Time";

        public float Duration { get => _duration; set { _duration = value; NotifyChanged(); } }
        public bool Loop { get => _loop; set { _loop = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            AddInputPort("start", "▶ Start", FluxPortType.Execution, "void", false);
            AddInputPort("stop", "▶ Stop", FluxPortType.Execution, "void", false);
            
            AddInputPort("duration", "Duration", FluxPortType.Data, "float", false, _duration);
            
            AddOutputPort("onTick", "▶ On Tick", FluxPortType.Execution, "void", false);
            AddOutputPort("onComplete", "▶ On Complete", FluxPortType.Execution, "void", false);
            
            AddOutputPort("progress", "Progress (0-1)", FluxPortType.Data, "float", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var context = executor.Runner.GetContextObject();
            if (context == null)
            {
                Debug.LogError("TimerNode requires a context GameObject to run.", this);
                return;
            }
            
            if (inputs.ContainsKey("start"))
            {
                StartTimer(executor, inputs);
            }
            if (inputs.ContainsKey("stop"))
            {
                StopTimer(context);
            }
        }

        private void StartTimer(FluxGraphExecutor executor, Dictionary<string, object> inputs)
        {
            var context = executor.Runner.GetContextObject();
            StopTimer(context); // Stop any existing timer for this context first.

            float duration = GetInputValue<float>(inputs, "duration", _duration);
            
            var monoBehaviour = context.GetComponent<MonoBehaviour>();
            if(monoBehaviour != null)
            {
                var coroutine = monoBehaviour.StartCoroutine(TimerCoroutine(executor, duration, _loop));
                _runningTimers[context] = coroutine;
            }
        }

        private void StopTimer(GameObject context)
        {
            if (_runningTimers.TryGetValue(context, out Coroutine coroutine))
            {
                var monoBehaviour = context.GetComponent<MonoBehaviour>();
                if (monoBehaviour != null && coroutine != null)
                {
                    monoBehaviour.StopCoroutine(coroutine);
                }
                _runningTimers.Remove(context);
            }
        }

        private IEnumerator TimerCoroutine(FluxGraphExecutor executor, float duration, bool loop)
        {
            var context = executor.Runner.GetContextObject();

            do
            {
                float elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    // If the context was destroyed mid-loop, stop the coroutine.
                    if (context == null) yield break;

                    elapsedTime += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsedTime / duration);

                    var tickOutputs = new Dictionary<string, object> { { "progress", progress } };
                    executor.ContinueFromPort(this, "onTick", tickOutputs);
                    
                    yield return null;
                }

                var completeOutputs = new Dictionary<string, object> { { "progress", 1f } };
                executor.ContinueFromPort(this, "onComplete", completeOutputs);

            } while (loop);
            
            if(context != null) _runningTimers.Remove(context);
        }
        
        protected void OnDestroy()
        {
            // This is a best-effort cleanup for the ScriptableObject asset itself.
            // The instance-based cleanup is handled by stopping the coroutine on the MonoBehaviour.
            _runningTimers.Clear();
        }
    }
}