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
    [FluxNode("Set UI Text", Category = "Framework/UI", Description = "Sets the text of a Text or TextMeshProUGUI component.")]
    public class SetTextNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Target", "The Text or TextMeshProUGUI component to modify.", PortCapacity.Single)]
        public Component Target;

        [Port(FluxPortDirection.Input, "Text", "The text to set on the target component.", PortCapacity.Single)]
        public string Text;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (Target == null) return;
            if (UITextAdapterHelper.TryGetAdapter(Target, out var adapter))
            {
                adapter.SetText(Text);
            }
        }
    }
}