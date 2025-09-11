using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A pure data node that performs a variety of mathematical operations on its inputs.
    /// </summary>
    [CreateAssetMenu(fileName = "MathNode", menuName = "Flux/Visual Scripting/Math/Math Operation")]
    public class MathNode : FluxNodeBase
    {
        [Tooltip("The mathematical operation to perform.")]
        [SerializeField] private MathOperation _operation = MathOperation.Add;

        public override string NodeName => $"Math ({_operation})";
        public override string Category => "Math";

        public MathOperation Operation 
        { 
            get => _operation; 
            set 
            { 
                _operation = value; 
                RefreshPorts(); // Refresh ports to show/hide the 'B' input
                NotifyChanged();
            } 
        }

        protected override void InitializePorts()
        {
            // Input A is always present.
            AddInputPort("a", "A", FluxPortType.Data, "float", true, 0f);

            // Input B is only needed for binary operations (like Add, Subtract, etc.).
            if (IsBinaryOperation(_operation))
            {
                AddInputPort("b", "B", FluxPortType.Data, "float", true, 0f);
            }
            
            AddOutputPort("result", "Result", FluxPortType.Data, "float", false);
        }

        /// <summary>
        /// Executes the mathematical calculation.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            float a = GetInputValue<float>(inputs, "a", 0f);
            // Only get 'b' if the operation requires it.
            float b = IsBinaryOperation(_operation) ? GetInputValue<float>(inputs, "b", 0f) : 0f;
            
            float result = _operation switch
            {
                // Binary Operations
                MathOperation.Add => a + b,
                MathOperation.Subtract => a - b,
                MathOperation.Multiply => a * b,
                MathOperation.Divide => (b != 0) ? a / b : float.PositiveInfinity, // Return Infinity for clarity
                MathOperation.Power => Mathf.Pow(a, b),
                MathOperation.Min => Mathf.Min(a, b),
                MathOperation.Max => Mathf.Max(a, b),
                
                // Unary Operations
                MathOperation.Abs => Mathf.Abs(a),
                MathOperation.Sin => Mathf.Sin(a),
                MathOperation.Cos => Mathf.Cos(a),
                MathOperation.Sqrt => (a >= 0) ? Mathf.Sqrt(a) : 0f, // Sqrt of negative is NaN, return 0 instead.
                
                _ => 0f
            };

            SetOutputValue(outputs, "result", result);
        }
        
        /// <summary>
        /// Helper to determine if an operation requires two inputs (A and B).
        /// </summary>
        private bool IsBinaryOperation(MathOperation op)
        {
            return op switch
            {
                MathOperation.Add or MathOperation.Subtract or MathOperation.Multiply or MathOperation.Divide or MathOperation.Power or MathOperation.Min or MathOperation.Max => true,
                _ => false,
            };
        }
    }

    public enum MathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Min,
        Max,
        Abs,
        Sin,
        Cos,
        Sqrt
    }
}