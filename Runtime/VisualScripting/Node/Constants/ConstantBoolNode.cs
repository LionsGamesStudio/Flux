using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Bool)", Category = "Data/Constants")]
    public class ConstantBoolNode : INode
    {
        [Tooltip("The value this node will output.")]
        public bool Value;
        
        [Port(FluxPortDirection.Output)]
        public bool Output;
        
        public void Execute() => Output = Value;
    }
}