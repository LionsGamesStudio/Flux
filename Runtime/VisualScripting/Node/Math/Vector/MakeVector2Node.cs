using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Make Vector2", Category = "Math/Vector", Description = "Creates a Vector2 from X and Y float values.")]
    public class MakeVector2Node : IExecutableNode
    {
        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "X", PortCapacity.Single)] public float X;
        [Port(FluxPortDirection.Input, "Y", PortCapacity.Single)] public float Y;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public Vector2 Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            Result = new Vector2(X, Y);
        }
    }
}