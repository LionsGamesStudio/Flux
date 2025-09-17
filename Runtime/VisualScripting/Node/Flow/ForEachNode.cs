using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("For Each", Category = "Flow", Description = "Executes a loop for each item in a collection, providing the item and its index.")]
    public class ForEachNode : IFlowControlNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;
        
        [Port(FluxPortDirection.Input, "Collection", "The list or array to iterate over.", PortCapacity.Single)]
        public IEnumerable collection;

        // --- Output Ports ---
        [Port(FluxPortDirection.Output, "Loop Body", portType: FluxPortType.Execution, "Executes once for each item in the collection.", PortCapacity.Multi)]
        public ExecutionPin LoopBody;

        [Port(FluxPortDirection.Output, "Completed", portType: FluxPortType.Execution, "Executes after the entire collection has been processed.", PortCapacity.Multi)]
        public ExecutionPin Completed;

        // Note: Output data ports are fields on the INode.
        // Their values will be set inside the token's local data.
        [Port(FluxPortDirection.Output, "Item", "The current item in the iteration.", PortCapacity.Multi)]
        public object item;
        
        [Port(FluxPortDirection.Output, "Index", "The index of the current item.", PortCapacity.Multi)]
        public int index;

        // This node takes control of the execution flow by returning a list of tokens.
        public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // Get the list of ALL nodes connected to the loop body port ONCE.
            var loopBodyNodes = wrapper.GetConnectedNodes(nameof(LoopBody)).ToList();

            if (collection != null)
            {
                int currentIndex = 0;
                foreach (var currentItem in collection)
                {
                    // If there are nodes connected to "Loop Body"...
                    if (loopBodyNodes.Any())
                    {
                        // ...iterate through ALL of them.
                        foreach (var loopNode in loopBodyNodes)
                        {
                            // Create a separate token for each connected node.
                            var loopToken = new ExecutionToken(loopNode);
                            loopToken.SetData(nameof(item), currentItem);
                            loopToken.SetData(nameof(index), currentIndex);
                            executor.ContinueFlow(loopToken, wrapper);
                        }
                    }
                    currentIndex++;
                }
            }
            
            // --- IMPORTANT ---
            // The flow to the "Completed" output is now also handled by this node.
            var completedNode = wrapper.GetConnectedNode(nameof(Completed));
            if (completedNode != null)
            {
                executor.ContinueFlow(new ExecutionToken(completedNode), wrapper);
            }
        }
    }
}