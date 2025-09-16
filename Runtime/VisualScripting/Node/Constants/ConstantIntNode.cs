using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Int)", Category = "Data/Constants")]
    public class ConstantIntNode : IExecutableNode
    {
        [Tooltip("The value this node will output.")]
        public int Value;
        
        [Port(FluxPortDirection.Output)]
        public int Output;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Output = Value;
    }
}