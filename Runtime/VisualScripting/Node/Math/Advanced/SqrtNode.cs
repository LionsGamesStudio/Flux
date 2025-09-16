using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Sqrt", Category = "Math", Description = "Calculates the square root of a number.")]
    public class SqrtNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "Value", PortCapacity.Single)]
        public float Value;

        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)]
        public float Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Result = Mathf.Sqrt(Value);
    }
}