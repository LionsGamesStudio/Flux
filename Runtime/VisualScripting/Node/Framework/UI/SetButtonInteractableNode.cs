using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.UI;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Set Button Interactable", Category = "Framework/UI", Description = "Enables or disables a UI Button.")]
    public class SetButtonInteractableNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;
        
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public Button Target;

        [Port(FluxPortDirection.Input, "Interactable", PortCapacity.Single)]
        public bool Interactable;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (Target != null)
            {
                Target.interactable = Interactable;
            }
        }
    }
}