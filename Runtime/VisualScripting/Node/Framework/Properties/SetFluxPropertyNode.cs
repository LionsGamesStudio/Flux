using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Set Flux Property", Category = "Framework/Properties", Description = "Sets the value of a reactive property in the FluxPropertyManager.")]
    public class SetFluxPropertyNode : IExecutableNode
    {
        [Tooltip("The unique key of the property to set.")]
        public string PropertyKey;

        // --- Execution Ports ---
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] 
        public ExecutionPin In;
        
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] 
        public ExecutionPin Out;

        // --- Data Input Port ---
        // The value type is 'object' to accept any kind of data.
        [Port(FluxPortDirection.Input, "Value", "The value to set the property to.", PortCapacity.Single)]
        public object Value;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (string.IsNullOrEmpty(PropertyKey) || Flux.Manager == null)
            {
                return;
            }

            var property = Flux.Manager.Properties.GetProperty(PropertyKey);
            if (property != null)
            {
                // The 'Value' field has already been populated by the executor
                // from the connected data port before this method was called.
                property.SetValue(this.Value);
            }
            else
            {
                Debug.LogWarning($"[SetFluxPropertyNode] Property with key '{PropertyKey}' not found.", wrapper);
            }
        }
    }
}