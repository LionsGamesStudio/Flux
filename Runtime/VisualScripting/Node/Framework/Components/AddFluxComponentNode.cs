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
    [FluxNode("Add Flux Component", Category = "Framework/Components", Description = "Adds a FluxMonoBehaviour of a specific type to a GameObject.")]
    public class AddFluxComponentNode : INode
    {
        [Tooltip("Specify the component type to add by dragging a component asset or a prefab with the component here.")]
        public FluxMonoBehaviour componentTypeReference;

        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public GameObject target;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        [Port(FluxPortDirection.Output, "Component", PortCapacity.Multi)]
        public FluxMonoBehaviour component;
        
        public void Execute(AttributedNodeWrapper wrapper)
        {
            this.component = null;
            if (target == null || componentTypeReference == null) return;

            Type typeToAdd = componentTypeReference.GetType();
            if (typeToAdd.IsAbstract)
            {
                Debug.LogError($"Add Flux Component Node: Cannot add an abstract component of type '{typeToAdd.Name}'.", wrapper);
                return;
            }
            
            this.component = target.AddComponent(typeToAdd) as FluxMonoBehaviour;
        }
    }
}