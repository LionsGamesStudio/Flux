using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using FluxFramework.Validation;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Make Range Validator (Float)", Category = "Framework/Validation", Description = "Creates a validator that ensures a float value is within a specified range.")]
    public class MakeRangeValidatorNode : IExecutableNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "Min", PortCapacity.Single)]
        public float Min = 0f;

        [Port(FluxPortDirection.Input, "Max", PortCapacity.Single)]
        public float Max = 1f;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Validator", PortCapacity.Multi)]
        public IValidator<float> Validator;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // The 'Min' and 'Max' fields are already populated by the executor.
            // This node simply creates the validator object.
            this.Validator = new RangeValidator<float>(this.Min, this.Max);
        }
    }
}