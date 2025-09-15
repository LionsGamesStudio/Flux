using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.UI;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Unbind UI Component", Category = "Framework/UI", Description = "Manually unregisters all data bindings on a FluxUIComponent.")]
    public class UnbindUIComponentNode : INode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Target", "The FluxUIComponent to unbind.", PortCapacity.Single)]
        public FluxUIComponent target;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        public void Execute()
        {
            target?.Unbind();
        }
    }
}