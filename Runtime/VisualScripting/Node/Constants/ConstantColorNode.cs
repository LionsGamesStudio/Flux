using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Color)", Category = "Data/Constants")]
    public class ConstantColorNode : INode
    {
        [Tooltip("The value this node will output.")]
        public Color Value;
        
        [Port(FluxPortDirection.Output)]
        public Color Output;
        
        public void Execute() => Output = Value;
    }
}