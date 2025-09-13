using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "LookAtNode", menuName = "Flux/Visual Scripting/Transform/Look At")]
    public class LookAtNode : FluxNodeBase
    {
        public override string NodeName => "Look At";
        public override string Category => "Transform";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("target", "Target", FluxPortType.Data, "Transform", true, null, "The Transform that will be rotated.");
            AddInputPort("lookAt", "Look At", FluxPortType.Data, "Vector3", true, Vector3.zero, "The world position to look at.");
            AddInputPort("up", "World Up", FluxPortType.Data, "Vector3", false, Vector3.up, "A vector defining the 'up' direction.");
            
            AddOutputPort("onComplete", "▶ Out", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<Transform>(inputs, "target");
            if (target != null)
            {
                var lookAtPosition = GetInputValue<Vector3>(inputs, "lookAt");
                var upVector = GetInputValue<Vector3>(inputs, "up", Vector3.up);
                target.LookAt(lookAtPosition, upVector);
            }

            SetOutputValue(outputs, "onComplete", null);
        }
    }
}