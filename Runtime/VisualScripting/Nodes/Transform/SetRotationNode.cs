using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "SetRotationNode", menuName = "Flux/Visual Scripting/Transform/Set Rotation")]
    public class SetRotationNode : FluxNodeBase
    {
        public override string NodeName => "Set Rotation";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to modify.");
            AddInputPort("rotation", "Rotation (Euler)", FluxPortType.Data, "Vector3", true);
            AddInputPort("space", "Space", FluxPortType.Data, "Space", false, Space.World, "Whether to set the rotation in World or Local space.");

            AddOutputPort("onSet", "▶ Out", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                var rotation = GetInputValue<Vector3>(inputs, "rotation");
                var space = GetInputValue<Space>(inputs, "space", Space.World);

                if (space == Space.World)
                {
                    target.eulerAngles = rotation;
                }
                else
                {
                    target.localEulerAngles = rotation;
                }
            }

            SetOutputValue(outputs, "onSet", null);
        }
    }
}