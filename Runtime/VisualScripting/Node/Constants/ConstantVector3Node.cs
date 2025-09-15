using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Vector3)", Category = "Data/Constants")]
    public class ConstantVector3Node : INode
    {
        [Tooltip("The value this node will output.")]
        public Vector3 Value;
        
        [Port(FluxPortDirection.Output)]
        public Vector3 Output;
        
        public void Execute() => Output = Value;
    }
}