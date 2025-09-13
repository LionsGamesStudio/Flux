using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "GetRotationNode", menuName = "Flux/Visual Scripting/Transform/Get Rotation")]
    public class GetRotationNode : FluxNodeBase
    {
        public override string NodeName => "Get Rotation";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform to read from.");
            AddInputPort("space", "Space", FluxPortType.Data, "Space", false, Space.World, "Whether to get the rotation in World or Local space.");

            AddOutputPort("eulerAngles", "Euler Angles", FluxPortType.Data, "Vector3", false);
            AddOutputPort("quaternion", "Quaternion", FluxPortType.Data, "Quaternion", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                var space = GetInputValue<Space>(inputs, "space", Space.World);
                if (space == Space.World)
                {
                    SetOutputValue(outputs, "eulerAngles", target.eulerAngles);
                    SetOutputValue(outputs, "quaternion", target.rotation);
                }
                else
                {
                    SetOutputValue(outputs, "eulerAngles", target.localEulerAngles);
                    SetOutputValue(outputs, "quaternion", target.localRotation);
                }
            }
        }
    }
}