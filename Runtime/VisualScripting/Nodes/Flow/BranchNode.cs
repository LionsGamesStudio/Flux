using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that directs the execution flow down one of two paths based on a boolean condition.
    /// It is the equivalent of an if-else statement.
    /// </summary>
    [CreateAssetMenu(fileName = "BranchNode", menuName = "Flux/Visual Scripting/Flow/Branch")]
    public class BranchNode : FluxNodeBase
    {
        public override string NodeName => "Branch";
        public override string Category => "Flow";

        protected override void InitializePorts()
        {
            // Execution flow
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddOutputPort("true", "▶ True", FluxPortType.Execution, "void", false);
            AddOutputPort("false", "▶ False", FluxPortType.Execution, "void", false);
            
            // Data input
            AddInputPort("condition", "Condition", FluxPortType.Data, "bool", true, false);
        }

        /// <summary>
        /// Executes the branch logic, triggering either the 'True' or 'False' output port.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            bool condition = GetInputValue<bool>(inputs, "condition", false);
            
            if (condition)
            {
                SetOutputValue(outputs, "true", null);
            }
            else
            {
                SetOutputValue(outputs, "false", null);
            }
        }
    }
}