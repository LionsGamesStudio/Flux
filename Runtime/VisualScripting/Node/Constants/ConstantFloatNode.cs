using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Float)", Category = "Data/Constants")]
    public class ConstantFloatNode : IExecutableNode
    {
        [Tooltip("The value this node will output.")]
        public float Value;
        
        [Port(FluxPortDirection.Output)]
        public float Output;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Output = Value;
    }
}