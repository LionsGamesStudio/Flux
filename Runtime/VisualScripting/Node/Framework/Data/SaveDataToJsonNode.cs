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
    [FluxNode("Save Data to JSON", Category = "Framework/Data", Description = "Serializes a FluxDataContainer asset to a JSON string.")]
    public class SaveDataToJsonNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Container", PortCapacity.Single)]
        public FluxDataContainer container;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        [Port(FluxPortDirection.Output, "JSON Data", PortCapacity.Multi)]
        public string jsonData;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (container != null)
            {
                jsonData = container.SerializeToJson();
            }
        }
    }
}