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
    public class DelayNode : IExecutableNode
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
            yield return new WaitForSeconds(duration);
            
            var nextNode = wrapper.GetConnectedNode(nameof(Out));
            if (nextNode != null)
            {
                // Create a new token and tell the executor to resume the flow with it.
                executor.ContinueFlow(new ExecutionToken(nextNode));
            }
        }
    }
}