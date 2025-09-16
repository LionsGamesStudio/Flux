using System;
using System.Collections;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("For Each", Category = "Flow", Description = "Executes a loop for each item in a collection, providing the item and its index.")]
    public class ForEachNode : INode
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
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            // The logic of this node is to generate NEW tokens.
            
            if (collection != null)
            {
                int currentIndex = 0;
                var loopBodyNode = wrapper.GetConnectedNode(nameof(LoopBody));

                foreach (var currentItem in collection)
                {
                    if (loopBodyNode != null)
                    {
                        var loopToken = new ExecutionToken(loopBodyNode);
                        loopToken.SetData(nameof(item), currentItem);
                        loopToken.SetData(nameof(index), currentIndex);
                        
                        // Queue the token for the loop body directly.
                        executor.ContinueFlow(loopToken);
                    }
                    currentIndex++;
                }
            }
            
            // if (collection != null)
            // {
            //     int currentIndex = 0;
            //     foreach (var currentItem in collection)
            //     {
            //         // Find the node connected to the "Loop Body" output port.
            //         var loopBodyNode = wrapper.GetConnectedNode(nameof(LoopBody));
            //         if (loopBodyNode != null)
            //         {
            //             // Create a new token destined for the start of the loop body.
            //             var loopToken = new ExecutionToken(loopBodyNode);

            //             // Store the item and index data *inside this specific token*.
            //             // We use the port name as the key for consistency.
            //             loopToken.SetData(nameof(item), currentItem);
            //             loopToken.SetData(nameof(index), currentIndex);

            //             // Yield this token to the executor.
            //             yield return loopToken;
            //         }
            //         currentIndex++;
            //     }
            // }

            // After all loop tokens have been yielded, yield a final token for the "Completed" path.
            // var completedNode = wrapper.GetConnectedNode(nameof(Completed));
            // if (completedNode != null)
            // {
            //     yield return new ExecutionToken(completedNode);
            // }
        }
    }
}