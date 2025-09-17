using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Compare (Float)", Category = "Logic/Comparison", Description = "Compares two float numbers.")]
    public class CompareFloatNode : IExecutableNode
    {
        public ComparisonOperation Operation = ComparisonOperation.Equal;
        [Port(FluxPortDirection.Input, "A", PortCapacity.Single)] public float A;
        [Port(FluxPortDirection.Input, "B", PortCapacity.Single)] public float B;
        [Port(FluxPortDirection.Output, "Result", PortCapacity.Multi)] public bool Result;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs) => Result = PerformComparison(A, B);

        private bool PerformComparison(float a, float b)
        {
            return Operation switch
            {
                ComparisonOperation.Equal => a == b,
                ComparisonOperation.NotEqual => a != b,
                ComparisonOperation.Greater => a > b,
                ComparisonOperation.GreaterEqual => a >= b,
                ComparisonOperation.Less => a < b,
                ComparisonOperation.LessEqual => a <= b,
                _ => false
            };
        }
    }
}