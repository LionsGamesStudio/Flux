using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Branch (Probability)", Category = "Flow", Description = "Splits the execution flow based on a configurable probability.")]
    public class BranchProbabilityNode : INode, IPortConfiguration
    {
        // --- Input Ports ---
        
        // We can now use a clean constructor for this execution port.
        [Port(FluxPortDirection.Input, "In", FluxPortType.Execution)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Chance (0-1)", FluxPortType.Data, "The probability for the 'True' path to be taken, from 0.0 to 1.0.")]
        [Range(0f, 1f)]
        public float Chance = 0.5f;

        // --- Output Ports ---
        
        [Port(FluxPortDirection.Output, "True", FluxPortType.Execution)]
        public ExecutionPin True;

        [Port(FluxPortDirection.Output, "False", FluxPortType.Execution)]
        public ExecutionPin False;

        // ... (Les méthodes Execute et ConfigurePorts restent exactement les mêmes)
        public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string portName) {}

        public void ConfigurePorts(AttributedNodeWrapper wrapper)
        {
            var truePort = wrapper.FindPort(nameof(True));
            if (truePort != null)
            {
                truePort.ProbabilityWeight = Chance;
            }

            var falsePort = wrapper.FindPort(nameof(False));
            if (falsePort != null)
            {
                falsePort.ProbabilityWeight = 1.0f - Chance;
            }
        }
    }
}