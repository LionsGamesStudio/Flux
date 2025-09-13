using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    [CreateAssetMenu(fileName = "GetComponentNode", menuName = "Flux/Visual Scripting/GameObject/Get Component")]
    public class GetComponentNode : FluxNodeBase
    {
        [Tooltip("The fully qualified name of the component type to get (e.g., 'UnityEngine.Rigidbody').")]
        [SerializeField] private string _componentTypeName = "UnityEngine.Transform";

        public override string NodeName => "Get Component";
        public override string Category => "GameObject/Components";

        public string ComponentTypeName
        {
            get => _componentTypeName;
            set
            {
                _componentTypeName = value;
                RefreshPorts(); // Update the output port type when the name changes
                NotifyChanged();
            }
        }
        
        protected override void InitializePorts()
        {
            AddInputPort("target", "Target", FluxPortType.Data, "GameObject", true, null, "The GameObject to search for the component on.");
            // This input allows overriding the type at runtime
            AddInputPort("typeName", "Type Name", FluxPortType.Data, "string", false, _componentTypeName);

            // The output port's type is dynamic
            string outputType = "UnityEngine.Component"; // Default
            if (!string.IsNullOrEmpty(_componentTypeName))
            {
                Type type = Type.GetType(_componentTypeName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                {
                    outputType = type.FullName;
                }
            }
            AddOutputPort("component", "Component", FluxPortType.Data, outputType);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var target = GetInputValue<GameObject>(inputs, "target");
            if (target == null)
            {
                Debug.LogWarning("GetComponentNode: Target GameObject is null.", this);
                SetOutputValue(outputs, "component", null);
                return;
            }

            string typeName = GetInputValue(inputs, "typeName", _componentTypeName);
            try
            {
                Type componentType = Type.GetType(typeName);
                if (componentType == null)
                {
                    Debug.LogError($"GetComponentNode: Component type '{typeName}' not found.", this);
                    SetOutputValue(outputs, "component", null);
                    return;
                }

                Component foundComponent = target.GetComponent(componentType);
                SetOutputValue(outputs, "component", foundComponent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GetComponentNode: Error getting component '{typeName}': {ex.Message}", this);
                SetOutputValue(outputs, "component", null);
            }
        }
    }
}