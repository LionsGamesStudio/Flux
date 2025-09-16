using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Listen for Flux Event", Category = "Framework/Events", Description = "An entry point that starts execution when a specific FluxEvent is published on the EventBus.")]
    public class ListenForFluxEventNode : IGraphAwakeNode, IPortConfiguration
    {
        [Tooltip("The type of FluxEvent to listen for.")]
        [SerializeField] 
        private string _eventTypeName;

        public void OnGraphAwake(FluxGraphExecutor executor, AttributedNodeWrapper wrapper)
        {
            if (string.IsNullOrEmpty(_eventTypeName)) return;

            var eventType = Type.GetType(_eventTypeName);
            if (eventType == null || !typeof(IFluxEvent).IsAssignableFrom(eventType))
            {
                Debug.LogError($"[ListenForFluxEventNode] Could not find or invalid event type: {_eventTypeName}", wrapper);
                return;
            }

            // Create a generic Action<T> where T is our specific eventType
            var actionType = typeof(Action<>).MakeGenericType(eventType);
            
            // This is the handler method. It captures the executor and wrapper.
            Action<IFluxEvent> handler = (evt) => {
                // This node is now the starting point of a new execution flow.
                var executionToken = new ExecutionToken(wrapper);
                
                // Pass event data to the token so the executor can populate the output ports
                var properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    // Exclude base properties for clarity
                    if (prop.DeclaringType != typeof(FluxEventBase))
                    {
                        executionToken.SetData(prop.Name, prop.GetValue(evt));
                    }
                }
                
                // Tell the executor to process this new token.
                executor.ContinueFlow(executionToken);
            };

            // Convert the handler to a delegate of the correct generic type
            var handlerDelegate = Delegate.CreateDelegate(actionType, handler.Target, handler.Method);

            // Use reflection to call EventBus.Subscribe<T>(Action<T> handler, int priority)
            var subscribeMethod = typeof(EventBus)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Subscribe" && m.GetParameters().Length == 2)
                .MakeGenericMethod(eventType);
                
            subscribeMethod.Invoke(null, new object[] { handlerDelegate, 0 });
        }

        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            // The node itself is the entry point, so it has an execution output
            yield return new CustomPortDefinition
            {
                PortName = "Out",
                Direction = FluxPortDirection.Output,
                PortType = FluxPortType.Execution,
                Capacity = PortCapacity.Multi,
                ValueTypeName = typeof(ExecutionPin).AssemblyQualifiedName
            };

            if (string.IsNullOrEmpty(_eventTypeName)) yield break;
            
            var eventType = Type.GetType(_eventTypeName);
            if (eventType == null) yield break;
            
            // Create a data output port for each public property of the event
            var properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                // Exclude base properties for clarity
                if (prop.DeclaringType == typeof(FluxEventBase)) continue;
                
                yield return new CustomPortDefinition
                {
                    PortName = prop.Name,
                    Direction = FluxPortDirection.Output,
                    PortType = FluxPortType.Data,
                    Capacity = PortCapacity.Multi,
                    ValueTypeName = prop.PropertyType.AssemblyQualifiedName
                };
            }
        }
    }
}