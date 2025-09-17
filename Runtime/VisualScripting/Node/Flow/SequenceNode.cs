using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Sequence", Category = "Flow", Description = "Executes a series of outputs in order, one after the other.")]
    public class SequenceNode : IFlowControlNode, IPortConfiguration
    {
        [Tooltip("The number of sequential output pins.")]
        public int OutputCount = 2;

        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // The logic is to fire each output pin in order.
            // Since our executor processes tokens sequentially within a frame,
            // we can just queue all the tokens at once.
            for (int i = 0; i < OutputCount; i++)
            {
                var portName = $"Then {i}";
                var connectedNodes = wrapper.GetConnectedNodes(portName);
                foreach (var node in connectedNodes)
                {
                    executor.ContinueFlow(new ExecutionToken(node), wrapper);
                }
            }
        }
        
        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            for (int i = 0; i < OutputCount; i++)
            {
                yield return new CustomPortDefinition
                {
                    PortName = $"Then {i}",
                    Direction = FluxPortDirection.Output,
                    PortType = FluxPortType.Execution,
                    Capacity = PortCapacity.Multi, // Each output can still split
                    ValueTypeName = typeof(ExecutionPin).AssemblyQualifiedName
                };
            }
        }
    }
}