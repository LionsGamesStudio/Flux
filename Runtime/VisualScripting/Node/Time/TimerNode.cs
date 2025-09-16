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
    [FluxNode("Timer", Category = "Time", Description = "Triggers an output every frame for a duration, then a completion output.")]
    public class TimerNode : IExecutableNode
    {
        private static readonly Dictionary<int, Coroutine> _runningTimers = new Dictionary<int, Coroutine>();

        public bool Loop = false;

        [Port(FluxPortDirection.Input, "Start", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin Start;
        
        [Port(FluxPortDirection.Input, "Stop", portType: FluxPortType.Execution, PortCapacity.Single )]
        public ExecutionPin Stop;
        
        [Port(FluxPortDirection.Input, "Duration (s)", PortCapacity.Single)]
        public float Duration = 1f;
        
        [Port(FluxPortDirection.Output, "On Tick", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin OnTick;

        [Port(FluxPortDirection.Output, "On Complete", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin OnComplete;

        [Port(FluxPortDirection.Output, "Progress (0-1)", PortCapacity.Multi)]
        public float Progress;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            var runnerMono = executor.Runner as MonoBehaviour;
            if (runnerMono == null) return;

            if (triggeredPortName == nameof(Start))
            {
                StopTimer(runnerMono);
                var coroutine = runnerMono.StartCoroutine(TimerCoroutine(executor, wrapper));
                _runningTimers[runnerMono.gameObject.GetInstanceID()] = coroutine;
            }
            else if (triggeredPortName == nameof(Stop))
            {
                StopTimer(runnerMono);
            }
        }

        private void StopTimer(MonoBehaviour runner)
        {
            int contextId = runner.gameObject.GetInstanceID();
            if (_runningTimers.TryGetValue(contextId, out Coroutine coroutine) && coroutine != null)
            {
                runner.StopCoroutine(coroutine);
                _runningTimers.Remove(contextId);
            }
        }

        private IEnumerator TimerCoroutine(FluxGraphExecutor executor, AttributedNodeWrapper wrapper)
        {
            var runnerGO = (executor.Runner as MonoBehaviour).gameObject;
            var onTickNode = wrapper.GetConnectedNode(nameof(OnTick));
            var onCompleteNode = wrapper.GetConnectedNode(nameof(OnComplete));

            do
            {
                float elapsedTime = 0f;
                while (elapsedTime < Duration)
                {
                    if (runnerGO == null) yield break;

                    elapsedTime += Time.deltaTime;
                    this.Progress = Mathf.Clamp01(elapsedTime / Duration);

                    if (onTickNode != null)
                    {
                        var tickToken = new ExecutionToken(onTickNode);
                        tickToken.SetData(nameof(Progress), this.Progress);
                        executor.ContinueFlow(tickToken);
                    }
                    
                    yield return null;
                }

                this.Progress = 1f;
                if (onTickNode != null)
                {
                    var finalTickToken = new ExecutionToken(onTickNode);
                    finalTickToken.SetData(nameof(Progress), this.Progress);
                    executor.ContinueFlow(finalTickToken);
                }
                
                if (onCompleteNode != null)
                {
                    executor.ContinueFlow(new ExecutionToken(onCompleteNode));
                }

            } while (Loop && runnerGO != null);
            
            if(runnerGO != null) _runningTimers.Remove(runnerGO.GetInstanceID());
        }
    }
}