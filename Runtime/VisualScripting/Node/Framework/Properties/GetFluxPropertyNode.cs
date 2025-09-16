using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Get Flux Property", Category = "Framework/Properties", Description = "Gets the value of a reactive property from the FluxPropertyManager.")]
    public class GetFluxPropertyNode : IExecutableNode
    {
        [Tooltip("The unique key of the property to get.")]
        public string PropertyKey;

        // Note: The value type is 'object' to be generic. The connection rules will handle compatibility.
        [Port(FluxPortDirection.Output, "Value", "The value of the specified property.", PortCapacity.Multi)] 
        public object Value;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            this.Value = null;
            if (string.IsNullOrEmpty(PropertyKey) || Flux.Manager == null)
            {
                return;
            }

            var property = Flux.Manager.Properties.GetProperty(PropertyKey);
            if (property != null)
            {
                this.Value = property.GetValue();
            }
            else
            {
                Debug.LogWarning($"[GetFluxPropertyNode] Property with key '{PropertyKey}' not found.", wrapper);
            }
        }
    }
}