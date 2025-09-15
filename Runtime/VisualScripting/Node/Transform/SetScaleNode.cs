using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Set Scale", Category = "Transform", Description = "Sets the local scale of a Transform.")]
    public class SetScaleNode : INode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] public ExecutionPin In;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Input, "Local Scale", PortCapacity.Single)] public Vector3 localScale;
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] public ExecutionPin Out;
        
        public void Execute()
        {
            if (target != null)
                target.localScale = localScale;
        }
    }
}