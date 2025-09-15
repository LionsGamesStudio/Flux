using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (GameObject)", Category = "Data/Constants")]
    public class ConstantGameObjectNode : INode
    {
        [Tooltip("The value this node will output.")]
        public GameObject Value;
        
        [Port(FluxPortDirection.Output)]
        public GameObject Output;
        
        public void Execute() => Output = Value;
    }
}