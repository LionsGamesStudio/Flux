using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Bool)", Category = "Data/Constants")]
    public class ConstantBoolNode : IExecutableNode
    {
        [Tooltip("The value this node will output.")]
        public bool Value;
        
        [Port(FluxPortDirection.Output)]
        public bool Output;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Output = Value;
    }
}