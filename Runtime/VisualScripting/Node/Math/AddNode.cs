using System;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Add", Category = "Math", Description = "Adds two float numbers together.")]
    public class AddNode : INode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)]
        public float A;

        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)]
        public float B;

        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)]
        public float Result;

        public void Execute()
        {
            Result = A + B;
        }
    }
}