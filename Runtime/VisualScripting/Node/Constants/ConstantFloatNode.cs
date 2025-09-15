using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Float)", Category = "Data/Constants")]
    public class ConstantFloatNode : INode
    {
        [Tooltip("The value this node will output.")]
        public float Value;
        
        [Port(FluxPortDirection.Output)]
        public float Output;
        
        public void Execute() => Output = Value;
    }
}