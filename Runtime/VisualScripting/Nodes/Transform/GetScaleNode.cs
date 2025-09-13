using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "GetScaleNode", menuName = "Flux/Visual Scripting/Transform/Get Scale")]
    public class GetScaleNode : FluxNodeBase
    {
        public override string NodeName => "Get Scale";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to read from.");
            AddOutputPort("scale", "Scale", FluxPortType.Data, "Vector3", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                SetOutputValue(outputs, "scale", target.localScale);
            }
        }
    }
}