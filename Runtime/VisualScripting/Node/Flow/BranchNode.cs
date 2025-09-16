using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Branch", Category = "Flow", Description = "If the condition is true, flow continues out of the 'True' port; otherwise, it continues out of the 'False' port.")]
    public class BranchNode : IFlowControlNode
    {
        // --- Execution Input ---
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)] 
        public ExecutionPin In;

        // --- Data Input ---
        [Port(FluxPortDirection.Input, "Condition", PortCapacity.Single)]
        public bool Condition;

        // --- Execution Outputs ---
        [Port(FluxPortDirection.Output, "True", portType: FluxPortType.Execution, PortCapacity.Multi)] 
        public ExecutionPin True;
        
        [Port(FluxPortDirection.Output, "False", portType: FluxPortType.Execution, PortCapacity.Multi)] 
        public ExecutionPin False;

        public void Execute(FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            string portToFollow = Condition ? nameof(True) : nameof(False);
            
            // Use the new plural method to handle potential flow splits.
            var nextNodes = wrapper.GetConnectedNodes(portToFollow);
            foreach (var nextNode in nextNodes)
            {
                executor.ContinueFlow(new ExecutionToken(nextNode));
            }
        }
    }
}