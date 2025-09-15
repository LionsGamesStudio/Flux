using System;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Get Flux Component", Category = "Framework/Components", Description = "Finds a specific component that inherits from FluxMonoBehaviour on a GameObject.")]
    public class GetFluxComponentNode : INode
    {
        [Tooltip("Specify the component type by dragging a component asset or a prefab with the component here.")]
        public FluxMonoBehaviour componentTypeReference;

        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public GameObject target;

        [Port(FluxPortDirection.Output, "Component", PortCapacity.Multi)]
        public FluxMonoBehaviour component;
        
        public void Execute(AttributedNodeWrapper wrapper)
        {
            this.component = null;
            if (target == null || componentTypeReference == null) return;
            
            Type typeToGet = componentTypeReference.GetType();
            this.component = target.GetComponent(typeToGet) as FluxMonoBehaviour;
        }
    }
}