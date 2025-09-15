using System;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("And", Category = "Logic/Boolean", Description = "Outputs true only if both A and B are true.")]
    public class AndNode : INode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)] public bool A;
        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)] public bool B;
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public bool Result;
        public void Execute() => Result = A && B;
    }
}