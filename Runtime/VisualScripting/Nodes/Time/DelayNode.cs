using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that pauses the execution flow for a specified duration in seconds.
    /// It uses a coroutine managed by the graph's runner.
    /// </summary>
    [CreateAssetMenu(fileName = "DelayNode", menuName = "Flux/Visual Scripting/Time/Delay")]
    public class DelayNode : FluxNodeBase
    {
        [Tooltip("The default delay duration in seconds.")]
        [SerializeField] private float _delay = 1f;
        
        public override string NodeName => "Delay";
        public override string Category => "Time";

        public float Delay { get => _delay; set { _delay = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("delay", "Seconds", FluxPortType.Data, "float", false, _delay);
            
            AddOutputPort("onComplete", "▶ Out", FluxPortType.Execution, "void", false);
        }

        /// <summary>
        /// Executes the delay by starting a coroutine on the graph runner.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            var runner = executor.Runner;
            var context = runner.GetContextObject();
            if (context == null)
            {
                Debug.LogError("DelayNode requires a context GameObject (via a runner) to start a coroutine.", this);
                return;
            }

            float delayTime = GetInputValue<float>(inputs, "delay", _delay);
            
            var monoBehaviour = context.GetComponent<MonoBehaviour>();
            if(monoBehaviour != null)
            {
                monoBehaviour.StartCoroutine(DelayCoroutine(executor, delayTime));
            }
            else
            {
                Debug.LogError("DelayNode: The context GameObject does not have a MonoBehaviour component to run the coroutine.", this);
            }
        }

        private IEnumerator DelayCoroutine(FluxGraphExecutor executor, float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            
            // After the delay, continue the graph execution from the 'onComplete' port.
            executor.ContinueFromPort(this, "onComplete", new Dictionary<string, object>());
        }
    }
}