using System;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Sin", Category = "Math", Description = "Calculates the sine of an angle.")]
    public class SinNode : INode
    {
        [Port(FluxPortDirection.Input, "Radians", PortCapacity.Single)]
        public float Radians;

        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)]
        public float Result;

        public void Execute() => Result = Mathf.Sin(Radians);
    }
}