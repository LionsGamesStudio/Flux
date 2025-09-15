using System;
using System.Linq;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Divide", Category = "Math", Description = "Divides two numbers.")]
    public class DivideNode : INode
    {
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)]
        public object A;

        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)]
        public object B;

        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)]
        public object Result;

        public void Execute()
        {
            try
            {
                if (A == null || B == null)
                {
                    Result = 0;
                    return;
                }

                double aVal = Convert.ToDouble(A);
                double bVal = Convert.ToDouble(B);

                if (Math.Abs(bVal) < double.Epsilon)
                {
                    Result = 0;
                    return;
                }

                Result = aVal / bVal;
            }
            catch
            {
                Result = 0;
            }
        }
    }
}