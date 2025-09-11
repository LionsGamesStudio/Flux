using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that serves as a primary entry point for a graph's execution.
    /// The FluxGraphExecutor will automatically find and run from this node.
    /// </summary>
    [CreateAssetMenu(fileName = "StartNode", menuName = "Flux/Visual Scripting/Flow/Start")]
    public class StartNode : FluxNodeBase
    {
        public override string NodeName => "Start";
        public override string Category => "Flow";

        public override Type GetEditorViewType()
        {
            return typeof(StartNode);
        }

        protected override void InitializePorts()
        {
            // This node only has one execution output.
            AddOutputPort("start", "▶ Start", FluxPortType.Execution, "void", false);
        }

        /// <summary>
        /// This method is called by the graph executor. Its only job is to trigger the output port.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            // It doesn't process any data, just signals that the execution flow should begin.
            SetOutputValue(outputs, "start", null);
        }

        /// <summary>
        /// Start nodes have no inputs, so they are always considered valid.
        /// </summary>
        public override bool Validate()
        {
            return true;
        }
    }
}