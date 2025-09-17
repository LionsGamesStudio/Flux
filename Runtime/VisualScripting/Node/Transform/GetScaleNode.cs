using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Scale", Category = "Transform", Description = "Gets the local scale of a Transform.")]
    public class GetScaleNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Output, "Local Scale", PortCapacity.Multi)] public Vector3 localScale;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (target != null)
                localScale = target.localScale;
        }
    }
}