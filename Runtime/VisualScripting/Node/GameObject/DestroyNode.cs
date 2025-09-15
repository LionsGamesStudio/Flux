using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Destroy", Category = "GameObject", Description = "Destroys a GameObject, optionally after a delay.")]
    public class DestroyNode : INode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public GameObject Target;

        [Port(FluxPortDirection.Input, "Delay (s)", PortCapacity.Single)]
        public float Delay = 0f;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        public void Execute()
        {
            if (Target != null)
            {
                UnityEngine.Object.Destroy(Target, Delay);
            }
        }
    }
}