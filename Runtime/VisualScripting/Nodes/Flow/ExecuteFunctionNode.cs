using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;
using System.Collections.Generic;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that executes another part of the graph as a function or sub-routine,
    /// starting from a specified target node.
    /// </summary>
    [CreateAssetMenu(fileName = "ExecuteFunctionNode", menuName = "Flux/Visual Scripting/Flow/Execute Function")]
    public class ExecuteFunctionNode : FluxNodeBase
    {
        [Tooltip("The entry node of the function/sub-graph to execute. Can be overridden by the input port.")]
        [SerializeField] private FluxNodeBase _targetNode;

        public override string NodeName => "Execute Function";
        public override string Category => "Framework/Flow Control";

        protected override void InitializePorts()
        {
            // Execution flow
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddOutputPort("onComplete", "▶ Out", FluxPortType.Execution, "void", false);

            // Data input for the target node
            AddInputPort("target", "Target Node", FluxPortType.Data, "FluxNodeBase", true, _targetNode, "The first node of the function to execute.");
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            // Only execute if the input execution port is triggered
            if (!inputs.ContainsKey("execute"))
            {
                return;
            }

            // Get the target node from the input port or the serialized field.
            var target = GetInputValue<FluxNodeBase>(inputs, "target", _targetNode);
            
            if (target != null)
            {
                // Use the executor to execute the sub-flow.
                executor.ExecuteSubFlow(target);
            }
            else
            {
                Debug.LogWarning("ExecuteFunctionNode: Target Node is not set.", this);
            }

            // Continue the main execution flow from the 'onComplete' port.
            SetOutputValue(outputs, "onComplete", null);
        }
    }
}