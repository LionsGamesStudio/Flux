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
    [FluxNode("Set Slider Value", Category = "Framework/UI", Description = "Sets the value of a UI Slider.")]
    public class SetSliderValueNode : INode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public Slider Target;

        [Port(FluxPortDirection.Input, "Value", PortCapacity.Single)]
        public float Value;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        public void Execute()
        {
            if (Target != null)
            {
                Target.value = Value;
            }
        }
    }
}