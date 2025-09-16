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
    [FluxNode("Load Data from JSON", Category = "Framework/Data", Description = "Loads data from a JSON string into a FluxDataContainer asset.")]
    public class LoadDataFromJsonNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Container", PortCapacity.Single)]
        public FluxDataContainer container;

        [Port(FluxPortDirection.Input, "JSON Data", PortCapacity.Single)]
        public string jsonData;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (container == null || string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning("Load Data Node: Container or JSON data is null.", wrapper);
                return;
            }
            container.LoadFromJson(jsonData);
        }
    }
}