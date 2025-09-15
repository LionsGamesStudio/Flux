using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Sub-Graph", Category = "Flow", Description = "Executes another graph asset as a function.")]
    public class SubGraphNode : INode, IPortConfiguration
    {
        [Tooltip("The graph asset to execute.")]
        public FluxVisualGraph subGraph;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, Execution.ExecutionToken incomingToken) { }

        public IEnumerable<CustomPortDefinition> GetDynamicPorts()
        {
            if (subGraph == null) yield break;
            
            // --- INPUTS for this SubGraphNode ---
            var inputNodes = subGraph.Nodes.OfType<AttributedNodeWrapper>()
                .Where(w => w.NodeLogic is GraphInputNode);
                
            foreach (var w in inputNodes)
            {
                var inputNodeLogic = (GraphInputNode)w.NodeLogic;
                // Create inputs on this node that match the outputs of the sub-graph's input node.
                foreach(var portDef in inputNodeLogic.Outputs)
                {
                    yield return new CustomPortDefinition {
                        PortName = portDef.PortName,
                        Direction = FluxPortDirection.Input, // Inverted direction
                        PortType = portDef.PortType,
                        Capacity = PortCapacity.Single,
                        ValueTypeName = (portDef.PortType == FluxPortType.Execution) ? typeof(ExecutionPin).AssemblyQualifiedName : portDef.ValueTypeName
                    };
                }
            }

            // --- OUTPUTS for this SubGraphNode ---
            var outputNodes = subGraph.Nodes.OfType<AttributedNodeWrapper>()
                .Where(w => w.NodeLogic is GraphOutputNode);

            foreach (var w in outputNodes)
            {
                var outputNodeLogic = (GraphOutputNode)w.NodeLogic;
                // Create outputs on this node that match the inputs of the sub-graph's output node.
                foreach(var portDef in outputNodeLogic.Inputs)
                {
                    yield return new CustomPortDefinition {
                        PortName = portDef.PortName,
                        Direction = FluxPortDirection.Output, // Inverted direction
                        PortType = portDef.PortType,
                        Capacity = PortCapacity.Multi,
                        ValueTypeName = (portDef.PortType == FluxPortType.Execution) ? typeof(ExecutionPin).AssemblyQualifiedName : portDef.ValueTypeName
                    };
                }
            }
        }
    }
}