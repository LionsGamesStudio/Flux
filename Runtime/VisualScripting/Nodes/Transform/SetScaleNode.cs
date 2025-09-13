using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "SetScaleNode", menuName = "Flux/Visual Scripting/Transform/Set Scale")]
    public class SetScaleNode : FluxNodeBase
    {
        public override string NodeName => "Set Scale";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to modify.");
            AddInputPort("scale", "Scale", FluxPortType.Data, "Vector3", true);
            AddOutputPort("onSet", "▶ Out", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                target.localScale = GetInputValue<Vector3>(inputs, "scale");
            }
            SetOutputValue(outputs, "onSet", null);
        }
    }
}