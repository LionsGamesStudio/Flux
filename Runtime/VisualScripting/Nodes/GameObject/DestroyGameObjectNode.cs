using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "DestroyGameObjectNode", menuName = "Flux/Visual Scripting/GameObject/Destroy")]
    public class DestroyGameObjectNode : FluxNodeBase
    {
        public override string NodeName => "Destroy";
        public override string Category => "GameObject/Lifecycle";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("target", "Target", FluxPortType.Data, "GameObject", true, null, "The GameObject to destroy.");
            AddInputPort("delay", "Delay (s)", FluxPortType.Data, "float", false, 0f, "Optional delay in seconds before destruction.");
            
            AddOutputPort("onDestroyed", "▶ Out", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<GameObject>(inputs, "target");
            if (target == null)
            {
                Debug.LogWarning("DestroyGameObjectNode: Target GameObject is null.", this);
            }
            else
            {
                float delay = GetInputValue<float>(inputs, "delay", 0f);
                GameObject.Destroy(target, delay);
            }

            SetOutputValue(outputs, "onDestroyed", null);
        }
    }
}