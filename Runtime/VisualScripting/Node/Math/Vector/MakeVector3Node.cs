using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Make Vector3", Category = "Math/Vector", Description = "Creates a Vector3 from X, Y, and Z float values.")]
    public class MakeVector3Node : IExecutableNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "X", PortCapacity.Single)] public float X;
        [Port(FluxPortDirection.Input, "Y", PortCapacity.Single)] public float Y;
        [Port(FluxPortDirection.Input, "Z", PortCapacity.Single)] public float Z;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public Vector3 Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            Result = new Vector3(X, Y, Z);
        }
    }
}