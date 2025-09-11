using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that loads or creates instances of FluxScriptableObjects.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxScriptableObjectNode", menuName = "Flux/Visual Scripting/Framework/Data/FluxScriptableObject")]
    public class FluxScriptableObjectNode : FluxNodeBase
    {
        [Tooltip("The action this node will perform.")]
        [SerializeField] private FluxScriptableObjectAction _action = FluxScriptableObjectAction.LoadFromResources;
        
        [Tooltip("The path within a 'Resources' folder to load the asset from.")]
        [SerializeField] private string _assetPath = "";
        
        [Tooltip("The fully qualified name of the type to create.")]
        [SerializeField] private string _typeName = "";

        public override string NodeName => $"ScriptableObject ({_action})";
        public override string Category => "Framework/Data";

        public FluxScriptableObjectAction Action 
        { 
            get => _action; 
            set { _action = value; RefreshPorts(); } 
        }

        public string AssetPath 
        { 
            get => _assetPath; 
            set 
            { 
                _assetPath = value; 
                NotifyChanged();
            } 
        }

        public string TypeName 
        { 
            get => _typeName; 
            set 
            { 
                _typeName = value; 
                NotifyChanged();
            } 
        }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "Execute", FluxPortType.Execution, "void", true);
            AddOutputPort("onSuccess", "On Success", FluxPortType.Execution, "void", false);
            AddOutputPort("onFailure", "On Failure", FluxPortType.Execution, "void", false);
            
            // --- DYNAMIC PORTS ---
            if (_action == FluxScriptableObjectAction.LoadFromResources)
            {
                AddInputPort("assetPath", "Resources Path", FluxPortType.Data, "string", true, _assetPath);
            }
            else if (_action == FluxScriptableObjectAction.CreateInstance)
            {
                AddInputPort("typeName", "Type Name", FluxPortType.Data, "string", true, _typeName);
            }
            
            AddOutputPort("scriptableObject", "ScriptableObject", FluxPortType.Data, "FluxScriptableObject", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            try
            {
                switch (_action)
                {
                    case FluxScriptableObjectAction.LoadFromResources:
                        LoadScriptableObject(inputs, outputs);
                        break;
                    case FluxScriptableObjectAction.CreateInstance:
                        CreateScriptableObject(inputs, outputs);
                        break;
                }
            }
            catch (Exception ex)
            {
                SetError(outputs, $"Error executing {_action}: {ex.Message}");
            }
        }

        private void LoadScriptableObject(Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            string assetPath = GetInputValue<string>(inputs, "assetPath", _assetPath);
            if (string.IsNullOrEmpty(assetPath))
            {
                SetError(outputs, "Asset Path is required.");
                return;
            }

            var asset = Resources.Load<FluxScriptableObject>(assetPath);
            if (asset != null)
            {
                SetOutputValue(outputs, "scriptableObject", asset);
                SetSuccess(outputs);
            }
            else
            {
                SetError(outputs, $"FluxScriptableObject not found in Resources at path: '{assetPath}'.");
            }
        }

        private void CreateScriptableObject(Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            string typeName = GetInputValue<string>(inputs, "typeName", _typeName);
            if (string.IsNullOrEmpty(typeName))
            {
                SetError(outputs, "Type Name is required.");
                return;
            }

            Type type = Type.GetType(typeName);
            if (type == null || !type.IsSubclassOf(typeof(FluxScriptableObject)))
            {
                SetError(outputs, $"Type '{typeName}' not found or is not a valid FluxScriptableObject.");
                return;
            }
            
            var instance = ScriptableObject.CreateInstance(type) as FluxScriptableObject;
            SetOutputValue(outputs, "scriptableObject", instance);
            SetSuccess(outputs);
        }

        private void SetSuccess(Dictionary<string, object> outputs)
        {
            SetOutputValue(outputs, "onSuccess", null);
        }

        private void SetError(Dictionary<string, object> outputs, string error)
        {
            SetOutputValue(outputs, "onFailure", null);
            Debug.LogError($"FluxScriptableObjectNode: {error}", this);
        }
    }

    /// <summary>
    /// Defines the safe actions the FluxScriptableObjectNode can perform at runtime.
    /// </summary>
    public enum FluxScriptableObjectAction
    {
        LoadFromResources,
        CreateInstance
    }
}