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
    [FluxNode("Get UI Text", Category = "Framework/UI", Description = "Gets the text from a Text or TextMeshProUGUI component.")]
    public class GetTextNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "Target", "The Text or TextMeshProUGUI component to read from.", PortCapacity.Single)]
        public Component Target;

        [Port(FluxPortDirection.Output, "Text", PortCapacity.Multi)]
        public string Text;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            this.Text = string.Empty;
            if (Target == null) return;
            if (UITextAdapterHelper.TryGetAdapter(Target, out var adapter))
            {
                this.Text = adapter.GetText();
            }
        }
    }
}