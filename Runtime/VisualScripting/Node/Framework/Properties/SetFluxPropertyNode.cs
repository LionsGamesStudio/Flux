using System;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Set Flux Property", Category = "Framework/Properties", Description = "Sets the value of a reactive property. Creates the property if it does not exist.")]
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
        [Port(FluxPortDirection.Input, "Value", "The value to set the property to.", PortCapacity.Single)]
        public object Value;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (string.IsNullOrEmpty(PropertyKey) || Flux.Manager == null)
            {
                return;
            }
            
            // The 'Value' field is populated by the executor before this method is called.
            if (this.Value == null)
            {
                // We can't create a property without knowing its type.
                // We could try to set an existing property to null, however.
                var existingProp = Flux.Manager.Properties.GetProperty(PropertyKey);
                if(existingProp != null)
                {
                    existingProp.SetValue(null);
                }
                else
                {
                    Debug.LogWarning($"[SetFluxPropertyNode] Input 'Value' is null for key '{PropertyKey}'. Cannot create a new property without a type.", wrapper);
                }
                return;
            }

            try
            {
                var properties = Flux.Manager.Properties;
                var valueType = this.Value.GetType();
                
                // Use reflection to find the generic GetOrCreateProperty<T> method.
                var getOrCreateMethod = typeof(IFluxPropertyManager)
                    .GetMethod("GetOrCreateProperty")
                    .MakeGenericMethod(valueType);
                
                // Invoke the method to get or create the property.
                // The second argument is the default value, which will be used only if the property is created for the first time.
                var property = (IReactiveProperty)getOrCreateMethod.Invoke(properties, new object[] { this.PropertyKey, this.Value });
                
                // Now that we are sure the property exists, set its value.
                // This ensures it's updated even if it already existed.
                property.SetValue(this.Value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SetFluxPropertyNode] Failed to get or create property '{PropertyKey}' with value of type '{this.Value.GetType().Name}'. Error: {e.Message}", wrapper);
            }
        }
    }
}