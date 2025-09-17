using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Position", Category = "Transform", Description = "Gets the world or local position of a Transform.")]
    public class GetPositionNode : IExecutableNode
    {
        public Space space = Space.World;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Output, "Position", PortCapacity.Multi)] public Vector3 position;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (target != null)
                position = (space == Space.World) ? target.position : target.localPosition;
        }
    }
}