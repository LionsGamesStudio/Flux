using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Float)", Category = "Data/Constants")]
    public class ConstantFloatNode : ConstantNode<float>
    {
        [Tooltip("The value this node will output.")]
        public float Value;
        
        [Port(FluxPortDirection.Output)]
        public float Output;
        
        public void Execute() => Output = Value;
    }
}