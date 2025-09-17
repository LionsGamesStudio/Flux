using System;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("On Start", Category = "Lifecycle", Description = "An entry point that executes once when the graph starts running.")]
    public class OnStartNode : INode
    {
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)] 
        public ExecutionPin Out;
    }
}