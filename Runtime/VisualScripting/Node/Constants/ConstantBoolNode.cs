using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Bool)", Category = "Data/Constants")]
    public class ConstantBoolNode : ConstantNode<bool>
    {
        [Tooltip("The value this node will output.")]
        public bool Value;
        
        [Port(FluxPortDirection.Output)]
        public bool Output;
        
        public void Execute() => Output = Value;
    }
}