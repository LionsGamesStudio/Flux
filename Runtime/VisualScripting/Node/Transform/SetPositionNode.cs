using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Set Position", Category = "Transform", Description = "Sets the world or local position of a Transform.")]
    public class SetPositionNode : INode
    {
        public Space space = Space.World;

        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] public ExecutionPin In;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Input, "Position", PortCapacity.Single)] public Vector3 position;
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] public ExecutionPin Out;
        
        public void Execute()
        {
            if (target != null)
            {
                if (space == Space.World) target.position = position;
                else target.localPosition = position;
            }
        }
    }
}