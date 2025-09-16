using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Set Rotation", Category = "Transform", Description = "Sets the world or local rotation of a Transform using Euler angles.")]
    public class SetRotationNode : IExecutableNode
    {
        public Space space = Space.World;
        
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] public ExecutionPin In;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Input, "Euler Angles", PortCapacity.Single)] public Vector3 eulerAngles;
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] public ExecutionPin Out;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (target != null)
            {
                if (space == Space.World) target.eulerAngles = eulerAngles;
                else target.localEulerAngles = eulerAngles;
            }
        }
    }
}