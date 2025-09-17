using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Publish Flux Event", Category = "Framework/Events", Description = "Constructs and publishes a FluxEvent on the EventBus.")]
    public class PublishFluxEventNode : IExecutableNode, IPortConfiguration
    {
        [Tooltip("The type of FluxEvent to publish. A custom editor should expose this as a dropdown.")]
        [SerializeField] 
        private string _eventTypeName;

        // --- Standard Execution Ports ---
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, capacity: PortCapacity.Single)] public ExecutionPin In;
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, capacity: PortCapacity.Single)] public ExecutionPin Out;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (string.IsNullOrEmpty(_eventTypeName)) return;

            var eventType = Type.GetType(_eventTypeName);
            if (eventType == null) return;
            
            // Find the constructor and its parameters
            var constructor = eventType.GetConstructors().FirstOrDefault();
            if (constructor == null)
            {
                Debug.LogError($"[PublishFluxEventNode] Event type '{eventType.Name}' has no public constructor.", wrapper);
                return;
            }

            // Assemble arguments from our data inputs
            var constructorParams = constructor.GetParameters();
            var args = new object[constructorParams.Length];
            for (int i = 0; i < constructorParams.Length; i++)
            {
                if (dataInputs.TryGetValue(constructorParams[i].Name, out var value))
                {
                    args[i] = value;
                }
                else
                {
                    // Use default value if an input is not connected
                    args[i] = constructorParams[i].ParameterType.IsValueType ? Activator.CreateInstance(constructorParams[i].ParameterType) : null;
                }
            }
            
            // Create the event instance
            var eventInstance = Activator.CreateInstance(eventType, args) as IFluxEvent;
            
            // Publish using reflection on the generic EventBus.Publish<T> method
            var publishMethod = typeof(EventBus)
                .GetMethod("Publish", BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(eventType);
                
            publishMethod.Invoke(null, new object[] { eventInstance });
        }

        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            if (string.IsNullOrEmpty(_eventTypeName)) yield break;
            
            var eventType = Type.GetType(_eventTypeName);
            if (eventType == null) yield break;

            var constructor = eventType.GetConstructors().FirstOrDefault();
            if (constructor == null) yield break;

            // Create a data input port for each parameter of the event's constructor
            foreach (var param in constructor.GetParameters())
            {
                yield return new CustomPortDefinition
                {
                    PortName = param.Name,
                    Direction = FluxPortDirection.Input,
                    PortType = FluxPortType.Data,
                    Capacity = PortCapacity.Single,
                    ValueTypeName = param.ParameterType.AssemblyQualifiedName
                };
            }
        }
    }
}