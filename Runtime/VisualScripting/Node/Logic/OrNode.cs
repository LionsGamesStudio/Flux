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
    [FluxNode("Or", Category = "Logic/Boolean", Description = "Outputs true if either A or B is true.")]
    public class OrNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)] public bool A;
        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)] public bool B;
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public bool Result;
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Result = A || B;
    }
}