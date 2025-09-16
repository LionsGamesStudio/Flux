using System;
using System.Collections.Generic;
using FluxFramework.Attributes;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using FluxFramework.Validation;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Make String Length Validator", Category = "Framework/Validation", Description = "Creates a validator that ensures a string's length is within a specified range.")]
    public class MakeStringLengthValidatorNode : IExecutableNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "Min Length", PortCapacity.Single)]
        public int MinLength = 0;

        [Port(FluxPortDirection.Input, "Max Length", PortCapacity.Single)]
        public int MaxLength = 100;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Validator", PortCapacity.Multi)]
        public IValidator<string> Validator;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // This node creates the validator object using a custom attribute class for consistency.
            // We can create a temporary attribute instance to pass to the validator's constructor.
            var tempAttr = new FluxStringLengthAttribute(this.MinLength, this.MaxLength);
            this.Validator = new StringLengthValidator(tempAttr);
        }
    }
}