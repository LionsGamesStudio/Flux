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
    [FluxNode("Not", Category = "Logic/Boolean", Description = "Outputs true if A is false.")]
    public class NotNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)] public bool A;
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public bool Result;
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Result = !A;
    }
}