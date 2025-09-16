using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Select", Category = "Logic", Description = "Based on a boolean condition, outputs one of two provided values.")]
    public class SelectNode : IExecutableNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "Condition", "If true, the 'If True' value is used. Otherwise, 'If False' is used.", PortCapacity.Single)]
        public bool Condition;

        [Port(FluxPortDirection.Input, "If True", "The value to output if the condition is true.", PortCapacity.Single)]
        public object IfTrue;

        [Port(FluxPortDirection.Input, "If False", "The value to output if the condition is false.", PortCapacity.Single)]
        public object IfFalse;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Result", "The selected value.", PortCapacity.Multi)]
        public object Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // The executor populates Condition, IfTrue, and IfFalse from connected ports.
            // This node's job is to select which one to pass to its Result field.
            Result = Condition ? IfTrue : IfFalse;
        }
    }
}