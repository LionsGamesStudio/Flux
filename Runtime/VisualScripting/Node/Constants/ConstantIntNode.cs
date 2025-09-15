using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Int)", Category = "Data/Constants")]
    public class ConstantIntNode : ConstantNode<int>
    {
        [Tooltip("The value this node will output.")]
        public int Value;
        
        [Port(FluxPortDirection.Output)]
        public int Output;
        
        public void Execute() => Output = Value;
    }
}