using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting.Node;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Branch (Probability)", Category = "Flow", Description = "Splits the execution flow based on a configurable probability.")]
    // This node now implements the new interface.
    public class BranchProbabilityNode : INode, IPortPostConfiguration
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "In", FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Chance (0-1)", FluxPortType.Data, PortCapacity.Single)]
        [Range(0f, 1f)]
        public float Chance = 0.5f;

        // --- Output Ports ---
        [Port(FluxPortDirection.Output, "True", FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin True;

        [Port(FluxPortDirection.Output, "False", FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin False;
        
        // The Execute method is trivial because the executor handles the probabilistic branching.
        public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName) {}

        /// <summary>
        /// This method is called by the wrapper AFTER the 'True' and 'False' ports have been created.
        /// Its job is to set their probability weights based on the 'Chance' field.
        /// </summary>
        public void PostConfigurePorts(AttributedNodeWrapper wrapper)
        {
            var truePort = wrapper.FindPort(nameof(True));
            if (truePort != null)
            {
                truePort.ProbabilityWeight = Chance;
            }

            var falsePort = wrapper.FindPort(nameof(False));
            if (falsePort != null)
            {
                // The weight of the failure path is the inverse of the success chance.
                falsePort.ProbabilityWeight = 1.0f - Chance;
            }
        }
    }
}