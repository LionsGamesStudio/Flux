using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Delay", Category = "Time", Description = "Pauses the execution flow for a specified duration in seconds.")]
    public class DelayNode : IFlowControlNode
    {
        [Port(FluxPortDirection.Input, portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Seconds", PortCapacity.Single)]
        public float duration = 1f;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        // This method starts a background process and returns nothing, ending the current execution path.
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            var runnerMono = executor.Runner as MonoBehaviour;
            if (runnerMono == null)
            {
                Debug.LogError("DelayNode requires its runner to be a MonoBehaviour to start a coroutine.", wrapper);
                return;
            }
            
            runnerMono.StartCoroutine(DelayCoroutine(executor, wrapper));
        }

        private IEnumerator DelayCoroutine(FluxGraphExecutor executor, AttributedNodeWrapper wrapper)
        {
            // We get the duration from the field, which has already been populated.
            // The field is also named 'duration', but to avoid confusion, let's use this.duration
            yield return new WaitForSeconds(this.duration);
            
            // Use the new plural method.
            var nextNodes = wrapper.GetConnectedNodes(nameof(Out));
            foreach (var nextNode in nextNodes)
            {
                executor.ContinueFlow(new ExecutionToken(nextNode), wrapper);
            }
        }
    }
}