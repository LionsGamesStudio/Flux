using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Reset Data", Category = "Framework/Data", Description = "Resets all reactive properties in a FluxDataContainer to their default values.")]
    public class ResetDataNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Container", PortCapacity.Single)]
        public FluxDataContainer container;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            container?.ResetReactiveProperties();
        }
    }
}