using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node for finding and interacting with instances of FluxMonoBehaviour.
    /// It uses safe, context-aware methods instead of global searches.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxMonoBehaviourNode", menuName = "Flux/Visual Scripting/Framework/Components/FluxMonoBehaviour")]
    public class FluxMonoBehaviourNode : FluxNodeBase
    {
        [Tooltip("The action to perform.")]
        [SerializeField] private FluxMonoBehaviourAction _action = FluxMonoBehaviourAction.GetComponent;
        
        [Tooltip("The fully qualified name of the specific FluxMonoBehaviour type to find or add.")]
        [SerializeField] private string _componentTypeName = "";

        public override string NodeName => $"Flux MonoBehaviour ({_action})";
        public override string Category => "Framework/Components";

        public FluxMonoBehaviourAction Action 
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

            if (_action != FluxMonoBehaviourAction.GetAll)
            {
                AddInputPort("componentType", "Component Type Name", FluxPortType.Data, "string", false, _componentTypeName);
            }
            if (_action == FluxMonoBehaviourAction.GetComponentInChildren || _action == FluxMonoBehaviourAction.GetComponentsInChildren)
            {
                AddInputPort("includeInactive", "Include Inactive", FluxPortType.Data, "bool", false, false);
            }
            
            AddOutputPort("component", "Component", FluxPortType.Data, "FluxMonoBehaviour", false);
            AddOutputPort("components", "Components", FluxPortType.Data, "FluxMonoBehaviour[]", false);
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
                    case FluxMonoBehaviourAction.GetComponent:
                        GetComponentOn(targetGO, componentTypeName, outputs);
                        break;
                    case FluxMonoBehaviourAction.AddComponent:
                        AddComponentTo(targetGO, componentTypeName, outputs);
                        break;
                    case FluxMonoBehaviourAction.GetComponentInChildren:
                        GetComponentIn(targetGO, componentTypeName, includeInactive, outputs);
                        break;
                    case FluxMonoBehaviourAction.GetComponentsInChildren:
                        GetComponentsIn(targetGO, componentTypeName, includeInactive, outputs);
                        break;
                    case FluxMonoBehaviourAction.GetAll:
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
            if (type == null || !type.IsSubclassOf(typeof(FluxMonoBehaviour)))
            {
                SetError(outputs, $"Type '{typeName}' not found or is not a valid FluxMonoBehaviour.");
                return null;
            }
            return type;
        }

        private void GetComponentOn(GameObject target, string typeName, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var component = target.GetComponent(type) as FluxMonoBehaviour;
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
            
            var component = target.AddComponent(type) as FluxMonoBehaviour;
            SetOutputValue(outputs, "component", component);
            SetSuccess(outputs);
        }

        private void GetComponentIn(GameObject target, string typeName, bool includeInactive, Dictionary<string, object> outputs)
        {
            Type type = GetValidType(typeName, outputs);
            if (type == null) return;
            
            var component = target.GetComponentInChildren(type, includeInactive) as FluxMonoBehaviour;
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
            var components = target.GetComponents<FluxMonoBehaviour>();
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
            Debug.LogError($"FluxMonoBehaviourNode: {error}", this);
        }
    }

    public enum FluxMonoBehaviourAction
    {
        GetComponent,
        GetAll,
        GetComponentInChildren,
        GetComponentsInChildren,
        AddComponent
    }
}