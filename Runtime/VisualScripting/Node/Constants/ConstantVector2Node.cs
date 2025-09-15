using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Vector2)", Category = "Data/Constants")]
    public class ConstantVector2Node : INode
    {
        [Tooltip("The value this node will output.")]
        public Vector2 Value;
        
        [Port(FluxPortDirection.Output)]
        public Vector2 Output;
        
        public void Execute() => Output = Value;
    }
}