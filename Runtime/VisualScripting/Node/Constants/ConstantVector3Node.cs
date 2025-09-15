using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (Vector3)", Category = "Data/Constants")]
    public class ConstantVector3Node : ConstantNode<Vector3>
    {
        [Tooltip("The value this node will output.")]
        public Vector3 Value;
        
        [Port(FluxPortDirection.Output)]
        public Vector3 Output;
        
        public void Execute() => Output = Value;
    }
}