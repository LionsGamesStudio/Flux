using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "SetPositionNode", menuName = "Flux/Visual Scripting/Transform/Set Position")]
    public class SetPositionNode : FluxNodeBase
    {
        public override string NodeName => "Set Position";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to modify.");
            AddInputPort("position", "Position", FluxPortType.Data, "Vector3", true);
            AddInputPort("space", "Space", FluxPortType.Data, "Space", false, Space.World, "Whether to set the position in World or Local space.");

            AddOutputPort("onSet", "▶ Out", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                var position = GetInputValue<Vector3>(inputs, "position");
                var space = GetInputValue<Space>(inputs, "space", Space.World);

                if (space == Space.World)
                {
                    target.position = position;
                }
                else
                {
                    target.localPosition = position;
                }
            }

            SetOutputValue(outputs, "onSet", null);
        }
    }
}