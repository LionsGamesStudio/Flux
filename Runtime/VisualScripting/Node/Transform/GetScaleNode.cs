using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Scale", Category = "Transform", Description = "Gets the local scale of a Transform.")]
    public class GetScaleNode : INode
    {
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Output, "Local Scale", PortCapacity.Multi)] public Vector3 localScale;
        
        public void Execute()
        {
            if (target != null)
                localScale = target.localScale;
        }
    }
}