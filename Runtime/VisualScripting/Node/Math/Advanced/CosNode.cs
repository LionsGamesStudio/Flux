using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Cos", Category = "Math", Description = "Calculates the cosine of an angle.")]
    public class CosNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "Radians", PortCapacity.Single)]
        public float Radians;

        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)]
        public float Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Result = Mathf.Cos(Radians);
    }
}