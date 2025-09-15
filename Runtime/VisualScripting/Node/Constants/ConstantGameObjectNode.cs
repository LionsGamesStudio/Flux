using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting
{
    [Serializable]
    [FluxNode("Constant (GameObject)", Category = "Data/Constants")]
    public class ConstantGameObjectNode : ConstantNode<GameObject>
    {
        [Tooltip("The value this node will output.")]
        public GameObject Value;
        
        [Port(FluxPortDirection.Output)]
        public GameObject Output;
        
        public void Execute() => Output = Value;
    }
}