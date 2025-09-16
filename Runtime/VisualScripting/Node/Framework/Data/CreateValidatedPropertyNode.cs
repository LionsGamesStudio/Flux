using System;
using System.Collections.Generic;
using System.Linq;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using FluxFramework.Extensions;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Create Validated Property (Float)", Category = "Framework/Data", Description = "Creates and registers a new validated reactive property of type Float.")]
    public class CreateValidatedPropertyFloatNode : IExecutableNode
    {
        [Tooltip("The unique key for the new property.")]
        public string PropertyKey;
        
        [Tooltip("Should this property be saved between sessions?")]
        public bool IsPersistent = false;

        // --- Execution Ports ---
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] 
        public ExecutionPin In;
        
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] 
        public ExecutionPin Out;

        // --- Data Input Ports ---
        [Port(FluxPortDirection.Input, "Default Value", PortCapacity.Single)]
        public float DefaultValue;

        [Port(FluxPortDirection.Input, "Validators", capacity: PortCapacity.Multi)] // This allows multiple connections
        public List<IValidator<float>> Validators; // The field type is a list to receive multiple inputs

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (string.IsNullOrEmpty(PropertyKey) || Flux.Manager == null)
            {
                return;
            }
            
            if (Flux.Manager.Properties.HasProperty(PropertyKey))
            {
                Debug.LogWarning($"[CreateValidatedProperty] Property with key '{PropertyKey}' already exists. Overwriting is not recommended at runtime.", wrapper);
            }

            // The 'Validators' list is populated by the executor from all connected validator ports.
            // The executor will create a list and add each connected validator to it.
            var newProperty = new ValidatedReactiveProperty<float>(DefaultValue, this.Validators);
            
            Flux.Manager.Properties.RegisterProperty(PropertyKey, newProperty, IsPersistent);
        }
    }
}