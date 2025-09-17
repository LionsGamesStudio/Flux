using System;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Relay", Category = "Utility", Description = "A simple passthrough node. Useful for rerouting connections or merging execution paths.")]
    public class RelayNode : IExecutableNode
    {
        // This input can receive multiple execution signals
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin In;

        // And it forwards them to its single output
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, System.Collections.Generic.Dictionary<string, object> dataInputs)
        {
            // This node does nothing. The executor will automatically continue the flow from its "Out" port.
        }
    }
}