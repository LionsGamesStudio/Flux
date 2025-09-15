using System;
using System.Linq;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Add", Category = "Math", Description = "Adds two numbers together.")]
    public class AddNode : INode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)]
        public object A;

        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)]
        public object B;

        [Port(FluxPortDirection.Output, "Result", FluxPortType.Data, PortCapacity.Multi)]
        public double Result;

        public void Execute()
        {
            try
            {
                if (A == null || B == null)
                {
                    Result = 0;
                    return;
                }

                // Convert both operands to double for calculation
                double aVal = Convert.ToDouble(A);
                double bVal = Convert.ToDouble(B);

                Result = aVal + bVal;
            }
            catch
            {
                // Fallback if conversion fails
                Result = 0;
            }
        }
    }
}