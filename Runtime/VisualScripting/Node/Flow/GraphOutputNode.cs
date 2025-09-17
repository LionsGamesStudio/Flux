using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Graph Output", Category = "Sub-Graphs", Description = "Defines an exit point and return values for a sub-graph.")]
    public class GraphOutputNode : IExecutableNode, IPortConfiguration
    {
        [Tooltip("Define the input ports for this exit point.")]
        public List<CustomPortDefinition> Inputs = new List<CustomPortDefinition>();

        // This node acts as a signal to the executor to return to the parent graph.
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) { }

        /// <summary>
        /// Defines the ports visible ON THIS NODE.
        /// As an output node, it needs INPUT ports to receive data and execution FROM the sub
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            foreach (var portDef in Inputs)
            {
                yield return new CustomPortDefinition
                {
                    PortName = portDef.PortName,
                    Direction = FluxPortDirection.Input, // Output nodes have INPUT ports
                    PortType = portDef.PortType,
                    Capacity = PortCapacity.Single,
                    ValueTypeName = (portDef.PortType == FluxPortType.Execution) ? typeof(ExecutionPin).AssemblyQualifiedName : portDef.ValueTypeName
                };
            }
        }
    }
}