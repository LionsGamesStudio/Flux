using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.UI;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node for finding and interacting with instances of FluxUIComponent.
    /// It uses safe, context-aware methods instead of global searches.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxUIComponentNode", menuName = "Flux/Visual Scripting/Framework/UI/FluxUIComponent")]
    public class FluxUIComponentNode : FluxNodeBase
    {
        [Tooltip("The action to perform.")]
        [SerializeField] private FluxUIComponentAction _action = FluxUIComponentAction.GetComponent;
        
        [Tooltip("The fully qualified name of the specific FluxUIComponent type to find or add.")]
        [SerializeField] private string _componentTypeName = "FluxFramework.UI.FluxText";

        public override string NodeName => $"Flux UI Component ({_action})";
        public override string Category => "Framework/UI";

        public FluxUIComponentAction Action 
        { 
            get => _action; 
            set { _action = value; RefreshPorts(); } 
        }

        public string ComponentTypeName 
        { 
            get => _componentTypeName; 
            set { _componentTypeName = value; NotifyChanged(); } 
        }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "Execute", FluxPortType.Execution, "void", true);
            AddOutputPort("onSuccess", "On Success", FluxPortType.Execution, "void", false);
            AddOutputPort("onFailure", "On Failure", FluxPortType.Execution, "void", false);
            
            AddInputPort("target", "Target GO", FluxPortType.Data, "GameObject", true);

            if (_action != FluxUIComponentAction.GetAll)
            {
                AddInputPort("componentType", "Component Type Name", FluxPortType.Data, "string", false, _componentTypeName);
            }
            if (_action == FluxUIComponentAction.GetComponentInChildren || _action == FluxUIComponentAction.GetComponentsInChildren)
            {
                AddInputPort("includeInactive", "Include Inactive", FluxPortType.Data, "bool", false, false);
            }
            
            AddOutputPort("component", "Component", FluxPortType.Data, "FluxUIComponent", false);
            AddOutputPort("components", "Components", FluxPortType.Data, "FluxUIComponent[]", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            GameObject targetGO = GetInputValue<GameObject>(inputs, "target");
            if (targetGO == null)
            {
                SetError(outputs, "Target GameObject is null.");
                return;
            }

            string componentTypeName = GetInputValue<string>(inputs, "componentType", _componentTypeName);
            bool includeInactive = GetInputValue<bool>(inputs, "includeInactive", false);

            try
            {
                switch (_action)
                {
                    case FluxUIComponentAction.GetComponent:
                        GetComponentOn(targetGO, componentTypeName, outputs);
                        break;
                    case FluxUIComponentAction.AddComponent:
                        AddComponentTo(targetGO, componentTypeName, outputs);
                        break;
                    case FluxUIComponentAction.GetComponentInChildren:
                        GetComponentIn(targetGO, componentTypeName, includeInactive, outputs);
                        break;
                    case FluxUIComponentAction.GetComponentsInChildren:
                        GetComponentsIn(targetGO, componentTypeName, includeInactive, outputs);
                        break;
                    case FluxUIComponentAction.GetAll:
                        GetAll(targetGO, outputs);
                        break;
                }
            }
            catch (Exception ex)
            {
                SetError(outputs, $"Error executing '{_action}': {ex.Message}");
            }
        }

        private Type GetValidType(string typeName, Dictionary<string, object> outputs)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                SetError(outputs, "Component Type Name is required for this action.");
                return null;
            }
            Type type = Type.GetType(typeName);
            if (type == null || !type.IsSubclassOf(typeof(FluxUIComponent)))
            {
                SetError(outputs, $"Type '{typeName}' not found or is not a valid FluxUIComponent.");
                return null;
            }
            return type;
        }

        private void GetComponentOn(GameObject target, string typeName, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var component = target.GetComponent(type) as FluxUIComponent;
            if (component != null)
            {
                SetOutputValue(outputs, "component", component);
                SetSuccess(outputs);
            }
            else
            {
                SetError(outputs, $"Component of type '{typeName}' not found on '{target.name}'.");
            }
        }

        private void AddComponentTo(GameObject target, string typeName, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var component = target.AddComponent(type) as FluxUIComponent;
            SetOutputValue(outputs, "component", component);
            SetSuccess(outputs);
        }

        private void GetComponentIn(GameObject target, string typeName, bool includeInactive, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var component = target.GetComponentInChildren(type, includeInactive) as FluxUIComponent;
            if (component != null)
            {
                SetOutputValue(outputs, "component", component);
                SetSuccess(outputs);
            }
            else
            {
                SetError(outputs, $"Component of type '{typeName}' not found in children of '{target.name}'.");
            }
        }

        private void GetComponentsIn(GameObject target, string typeName, bool includeInactive, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var components = target.GetComponentsInChildren(type, includeInactive);
            SetOutputValue(outputs, "components", components);
            SetSuccess(outputs);
        }

        private void GetAll(GameObject target, Dictionary<string, object> outputs)
        {
            var components = target.GetComponents<FluxUIComponent>();
            SetOutputValue(outputs, "components", components);
            SetSuccess(outputs);
        }
        
        private void SetSuccess(Dictionary<string, object> outputs)
        {
            SetOutputValue(outputs, "onSuccess", null);
        }

        private void SetError(Dictionary<string, object> outputs, string error)
        {
            SetOutputValue(outputs, "onFailure", null);
            Debug.LogError($"FluxUIComponentNode: {error}", this);
        }
    }

    public enum FluxUIComponentAction
    {
        GetComponent,
        GetAll, // Finds all FluxUIComponents on the target
        GetComponentInChildren,
        GetComponentsInChildren,
        AddComponent
    }
}