using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Component", Category = "GameObject", Description = "Gets a component of a specific type from a GameObject.")]
    public class GetComponentNode : INode
    {
        [Tooltip("Specify the component type by dragging an asset or prefab with the component here.")]
        public Component componentTypeReference;

        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public GameObject Target;

        [Port(FluxPortDirection.Output, "Component", PortCapacity.Multi)]
        public Component Component;

        public void Execute(AttributedNodeWrapper wrapper)
        {
            this.Component = null;
            if (Target == null || componentTypeReference == null) return;
            
            Type typeToGet = componentTypeReference.GetType();
            this.Component = Target.GetComponent(typeToGet);
        }
    }
}