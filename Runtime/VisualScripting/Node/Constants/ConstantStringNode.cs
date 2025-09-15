using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (String)", Category = "Data/Constants")]
    public class ConstantStringNode : ConstantNode<string>
    {
        [Tooltip("The value this node will output.")]
        public string Value;
        
        [Port(FluxPortDirection.Output)]
        public string Output;
        
        public void Execute() => Output = Value;
    }
}