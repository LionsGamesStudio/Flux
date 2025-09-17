using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Instantiate", Category = "GameObject", Description = "Creates an instance of a Prefab.")]
    public class InstantiateNode : IExecutableNode
    {
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;
        
        [Port(FluxPortDirection.Input, "Prefab", PortCapacity.Single)]
        public GameObject Prefab;

        [Port(FluxPortDirection.Input, "Position", PortCapacity.Single)]
        public Vector3 Position;

        [Port(FluxPortDirection.Input, "Rotation", PortCapacity.Single)]
        public Quaternion Rotation = Quaternion.identity;

        [Port(FluxPortDirection.Input, "Parent", PortCapacity.Single)]
        public Transform Parent;

        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;

        [Port(FluxPortDirection.Output, "Instance", PortCapacity.Multi)]
        public GameObject Instance;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (Prefab == null)
            {
                Debug.LogError("Instantiate Node: Prefab input is null.", wrapper);
                return;
            }
            Instance = UnityEngine.Object.Instantiate(Prefab, Position, Rotation, Parent);
        }
    }
}