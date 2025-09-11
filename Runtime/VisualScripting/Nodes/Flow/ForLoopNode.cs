using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that provides 'for loop' functionality, executing its body for each index.
    /// This implementation uses a coroutine to prevent blocking the main thread.
    /// </summary>
    [CreateAssetMenu(fileName = "ForLoopNode", menuName = "Flux/Visual Scripting/Flow/For Loop")]
    public class ForLoopNode : FluxNodeBase
    {
        [SerializeField] private int _startIndex = 0;
        [SerializeField] private int _endIndex = 10;
        [SerializeField] private int _step = 1;

        public override string NodeName => "For Loop";
        public override string Category => "Flow";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ Start", FluxPortType.Execution, "void", true);
            AddInputPort("startIndex", "Start", FluxPortType.Data, "int", false, _startIndex);
            AddInputPort("endIndex", "End", FluxPortType.Data, "int", false, _endIndex);
            AddInputPort("step", "Step", FluxPortType.Data, "int", false, _step);

            AddOutputPort("loopBody", "▶ Loop Body", FluxPortType.Execution, "void", false);
            AddOutputPort("onComplete", "▶ Completed", FluxPortType.Execution, "void", false);

            AddOutputPort("index", "Index", FluxPortType.Data, "int", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            var context = executor.Runner.GetContextObject();
            if (context == null)
            {
                Debug.LogError("ForLoopNode requires a context GameObject to run its coroutine.", this);
                return;
            }

            int start = GetInputValue<int>(inputs, "startIndex", _startIndex);
            int end = GetInputValue<int>(inputs, "endIndex", _endIndex);
            int step = GetInputValue<int>(inputs, "step", _step);

            // Start the loop as a coroutine on the executor's GameObject
            context.GetComponent<MonoBehaviour>().StartCoroutine(LoopCoroutine(executor, start, end, step));
        }

        private IEnumerator LoopCoroutine(FluxGraphExecutor executor, int start, int end, int step)
        {
            if (step == 0)
            {
                Debug.LogError("ForLoopNode: Step cannot be zero.", this);
                yield break;
            }

            if (step > 0)
            {
                for (int i = start; i < end; i += step)
                {
                    var loopOutputs = new Dictionary<string, object> { { "index", i } };
                    executor.ContinueFromPort(this, "loopBody", loopOutputs);
                    // Wait for the end of the frame to allow the loop body graph to execute
                    // before starting the next iteration.
                    yield return null;
                }
            }
            else // Negative step
            {
                for (int i = start; i > end; i += step)
                {
                    var loopOutputs = new Dictionary<string, object> { { "index", i } };
                    executor.ContinueFromPort(this, "loopBody", loopOutputs);
                    yield return null;
                }
            }

            // When the loop is finished, trigger the onComplete port.
            executor.ContinueFromPort(this, "onComplete", new Dictionary<string, object>());
        }
    }
}
  