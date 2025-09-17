using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Validate Data", Category = "Framework/Data", Description = "Validates the business logic rules of a FluxDataContainer.")]
    public class ValidateDataNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Container", PortCapacity.Single)]
        public FluxDataContainer container;

        [Port(FluxPortDirection.Output, "On Valid", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin OnValid;

        [Port(FluxPortDirection.Output, "On Invalid", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin OnInvalid;

        [Port(FluxPortDirection.Output, "Error Messages", PortCapacity.Multi)]
        public List<string> errorMessages;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (container == null)
            {
                executor.ContinueFlow(new ExecutionToken(wrapper.GetConnectedNode(nameof(OnInvalid))), wrapper);
                return;
            }

            if (container.ValidateData(out this.errorMessages))
            {
                executor.ContinueFlow(new ExecutionToken(wrapper.GetConnectedNode(nameof(OnValid))), wrapper);
            }
            else
            {
                executor.ContinueFlow(new ExecutionToken(wrapper.GetConnectedNode(nameof(OnInvalid))), wrapper);
            }
        }
    }
}