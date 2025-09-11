using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A pure data node that performs a logical comparison between two input values (A and B).
    /// </summary>
    [CreateAssetMenu(fileName = "ComparisonNode", menuName = "Flux/Visual Scripting/Logic/Comparison")]
    public class ComparisonNode : FluxNodeBase
    {
        [Tooltip("The type of comparison to perform.")]
        [SerializeField] private ComparisonOperation _operation = ComparisonOperation.Equal;

        public override string NodeName => $"Compare ({_operation})";
        public override string Category => "Logic";

        public ComparisonOperation Operation 
        { 
            get => _operation; 
            set 
            { 
                _operation = value; 
                NotifyChanged();
            } 
        }

        protected override void InitializePorts()
        {
            // Data inputs. They are not required as they can use default null values.
            AddInputPort("a", "A", FluxPortType.Data, "object", false, null);
            AddInputPort("b", "B", FluxPortType.Data, "object", false, null);
            
            // Data output.
            AddOutputPort("result", "Result", FluxPortType.Data, "bool", false);
        }

        /// <summary>
        /// Executes the comparison logic. This node does not have an execution flow; it is
        /// executed automatically by the graph runner when a connected node needs its 'Result' value.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            
            object a = GetInputValue<object>(inputs, "a");
            object b = GetInputValue<object>(inputs, "b");
            
            bool result = CompareValues(a, b, _operation);
            
            SetOutputValue(outputs, "result", result);
        }

        private bool CompareValues(object a, object b, ComparisonOperation operation)
        {
            // Handle null comparisons first for robustness.
            if (a == null && b == null) return operation == ComparisonOperation.Equal || operation == ComparisonOperation.GreaterEqual || operation == ComparisonOperation.LessEqual;
            if (a == null || b == null) return operation == ComparisonOperation.NotEqual;

            // Try to compare as numbers first, as it's the most common case.
            if (TryNumericComparison(a, b, operation, out bool numericResult))
            {
                return numericResult;
            }

            // If not numeric, try to compare as IComparable (strings, dates, etc.)
            if (a is IComparable compA && b is IComparable compB && a.GetType() == b.GetType())
            {
                int comparison = compA.CompareTo(compB);
                return operation switch
                {
                    ComparisonOperation.Equal => comparison == 0,
                    ComparisonOperation.NotEqual => comparison != 0,
                    ComparisonOperation.Greater => comparison > 0,
                    ComparisonOperation.GreaterEqual => comparison >= 0,
                    ComparisonOperation.Less => comparison < 0,
                    ComparisonOperation.LessEqual => comparison <= 0,
                    _ => false
                };
            }

            // Fallback to object.Equals for equality checks.
            return operation switch
            {
                ComparisonOperation.Equal => a.Equals(b),
                ComparisonOperation.NotEqual => !a.Equals(b),
                // Greater/Less comparisons are not meaningful for generic objects, so they return false.
                _ => false 
            };
        }

        private bool TryNumericComparison(object a, object b, ComparisonOperation operation, out bool result)
        {
            result = false;
            
            if (!IsNumeric(a) || !IsNumeric(b))
            {
                return false;
            }

            try
            {
                // Use decimal for higher precision comparisons to avoid floating point issues.
                decimal numA = Convert.ToDecimal(a);
                decimal numB = Convert.ToDecimal(b);

                result = operation switch
                {
                    ComparisonOperation.Equal => numA == numB,
                    ComparisonOperation.NotEqual => numA != numB,
                    ComparisonOperation.Greater => numA > numB,
                    ComparisonOperation.GreaterEqual => numA >= numB,
                    ComparisonOperation.Less => numA < numB,
                    ComparisonOperation.LessEqual => numA <= numB,
                    _ => false
                };

                return true;
            }
            catch (OverflowException)
            {
                // Fallback to double if decimal overflows (e.g., for very large floats)
                double numA = Convert.ToDouble(a);
                double numB = Convert.ToDouble(b);

                result = operation switch
                {
                    ComparisonOperation.Equal => Math.Abs(numA - numB) < double.Epsilon,
                    ComparisonOperation.NotEqual => Math.Abs(numA - numB) >= double.Epsilon,
                    ComparisonOperation.Greater => numA > numB,
                    ComparisonOperation.GreaterEqual => numA >= numB,
                    ComparisonOperation.Less => numA < numB,
                    ComparisonOperation.LessEqual => numA <= numB,
                    _ => false
                };
                
                return true;
            }
        }

        private bool IsNumeric(object value)
        {
            return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }
    }

    public enum ComparisonOperation
    {
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual
    }
}