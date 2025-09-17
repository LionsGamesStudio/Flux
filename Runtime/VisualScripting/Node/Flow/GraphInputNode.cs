using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Graph Input", Category = "Sub-Graphs", Description = "Defines the entry point and input parameters for a sub-graph.")]
    public class GraphInputNode : IExecutableNode, IPortConfiguration
    {
        [Tooltip("Define the output ports for this entry point.")]
        public List<CustomPortDefinition> Outputs = new List<CustomPortDefinition>();

        // This node's logic is to simply pass through the data it receives from the parent graph.
        // The executor will handle populating its fields before the sub-graph flow begins.
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) { }

        /// <summary>
        /// Defines the ports visible ON THIS NODE.
        /// As an input node, it needs OUTPUT ports to send data and execution INTO the sub
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            foreach (var portDef in Outputs)
            {
                yield return new CustomPortDefinition
                {
                    PortName = portDef.PortName,
                    Direction = FluxPortDirection.Output,
                    PortType = portDef.PortType,
                    Capacity = PortCapacity.Multi, // Outputs are Multi by default
                    ValueTypeName = (portDef.PortType == FluxPortType.Execution) ? typeof(ExecutionPin).AssemblyQualifiedName : portDef.ValueTypeName
                };
            }
        }
    }
}