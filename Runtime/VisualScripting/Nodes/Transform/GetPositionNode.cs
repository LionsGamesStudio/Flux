using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "GetPositionNode", menuName = "Flux/Visual Scripting/Transform/Get Position")]
    public class GetPositionNode : FluxNodeBase
    {
        public override string NodeName => "Get Position";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to read from.");
            AddInputPort("space", "Space", FluxPortType.Data, "Space", false, Space.World, "Whether to get the position in World or Local space.");

            AddOutputPort("position", "Position", FluxPortType.Data, "Vector3", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                var space = GetInputValue<Space>(inputs, "space", Space.World);
                Vector3 position = (space == Space.World) ? target.position : target.localPosition;
                SetOutputValue(outputs, "position", position);
            }
        }
    }
}