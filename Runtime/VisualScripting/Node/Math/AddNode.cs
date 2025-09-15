using System;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Add", Category = "Math", Description = "Adds two float numbers together.")]
    public class AddNode : INode
    {
        [Port(FluxPortDirection.Input, "A")]
        public float A;

        [Port(FluxPortDirection.Input, "B")]
        public float B;

        [Port(FluxPortDirection.Output, "Result")]
        public float Result;

        public void Execute()
        {
            Result = A + B;
        }
    }
}