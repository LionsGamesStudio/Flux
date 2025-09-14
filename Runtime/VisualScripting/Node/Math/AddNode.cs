using System;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Add", Category = "Math", Description = "Adds two float numbers together.")]
    public class AddNode : INode
    {
        [Port(FluxPortDirection.Input)]
        public float A;

        [Port(FluxPortDirection.Input)]
        public float B;

        [Port(FluxPortDirection.Output)]
        public float Result;

        public void Execute()
        {
            Result = A + B;
        }
    }
}