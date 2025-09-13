using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "InstantiateGameObjectNode", menuName = "Flux/Visual Scripting/GameObject/Instantiate")]
    public class InstantiateGameObjectNode : FluxNodeBase
    {
        public override string NodeName => "Instantiate";
        public override string Category => "GameObject/Lifecycle";

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("prefab", "Prefab", FluxPortType.Data, "GameObject", true, null, "The GameObject or Prefab to instantiate.");
            AddInputPort("position", "Position", FluxPortType.Data, "Vector3", false, Vector3.zero);
            AddInputPort("rotation", "Rotation", FluxPortType.Data, "Quaternion", false, Quaternion.identity);
            AddInputPort("parent", "Parent", FluxPortType.Data, "Transform", false, null, "Optional parent for the new object.");
            
            AddOutputPort("onInstantiated", "▶ Out", FluxPortType.Execution, "void", false);
            AddOutputPort("instance", "Instance", FluxPortType.Data, "GameObject", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var prefab = GetInputValue<GameObject>(inputs, "prefab");
            if (prefab == null)
            {
                Debug.LogError("InstantiateGameObjectNode: Prefab is null.", this);
                return; // Do not continue execution
            }

            var position = GetInputValue<Vector3>(inputs, "position", Vector3.zero);
            var rotation = GetInputValue<Quaternion>(inputs, "rotation", Quaternion.identity);
            var parent = GetInputValue<Transform>(inputs, "parent");

            var instance = GameObject.Instantiate(prefab, position, rotation, parent);
            
            SetOutputValue(outputs, "instance", instance);
            SetOutputValue(outputs, "onInstantiated", null); // Signal to continue execution
        }
    }
}