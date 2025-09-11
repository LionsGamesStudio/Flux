using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that interacts with FluxDataContainer assets.
    /// It allows for serialization, deserialization, validation, and resetting of data containers.
    /// </summary>
    [CreateAssetMenu(fileName = "DataContainerNode", menuName = "Flux/Visual Scripting/Framework/Data/Data Container")]
    public class DataContainerNode : FluxNodeBase
    {
        [Tooltip("The default Data Container asset to use if none is provided via the input port.")]
        [SerializeField] private FluxDataContainer _dataContainer;
        [Tooltip("The action this node will perform on the data container.")]
        [SerializeField] private DataContainerAction _action = DataContainerAction.SaveToJson;

        public override string NodeName => $"Data Container ({_action})";
        public override string Category => "Framework/Data";

        public FluxDataContainer DataContainer 
        { 
            get => _dataContainer; 
            set { _dataContainer = value; NotifyChanged(); } 
        }

        public DataContainerAction Action 
        { 
            get => _action; 
            set { _action = value; RefreshPorts(); } // Refresh ports when action changes
        }
        
        /// <summary>
        /// Validates the overall consistency and business logic of the data in this container.
        /// This is intended for rules that involve multiple properties.
        /// Individual property validation is handled automatically by ValidatedReactiveProperty.
        /// </summary>
        /// <param name="errorMessages">A list of detailed error messages if validation fails.</param>
        /// <returns>True if the container's data is consistent and valid, otherwise false.</returns>
        public virtual bool ValidateData(out List<string> errorMessages)
        {
            // The base implementation has no rules, so it's always valid.
            // Child classes should override this to add their specific business logic.
            errorMessages = new List<string>();
            return true;
        }

        protected override void InitializePorts()
        {
            // Execution ports (always present)
            AddInputPort("execute", "Execute", FluxPortType.Execution, "void", true);
            AddOutputPort("onSuccess", "On Success", FluxPortType.Execution, "void", false);
            AddOutputPort("onFailure", "On Failure", FluxPortType.Execution, "void", false);

            // --- DYNAMIC DATA PORTS based on Action ---

            // Input port for the container itself
            AddInputPort("dataContainer", "Data Container", FluxPortType.Data, "FluxDataContainer", true, _dataContainer);

            // Input port for JSON data when loading
            if (_action == DataContainerAction.LoadFromJson)
            {
                AddInputPort("json", "JSON Data", FluxPortType.Data, "string", true);
            }

            // --- DATA OUTPUTS ---

            // Output port for the generated JSON when saving
            if (_action == DataContainerAction.SaveToJson)
            {
                AddOutputPort("json", "JSON Data", FluxPortType.Data, "string", false);
            }

            // General output ports
            AddOutputPort("success", "Success", FluxPortType.Data, "bool", false);

            // Additional outputs for validation results
            if (_action == DataContainerAction.Validate)
            {
                AddOutputPort("isValid", "Is Valid", FluxPortType.Data, "bool", false);
                AddOutputPort("errorMessages", "Errors", FluxPortType.Data, "List<string>", false);
            }
        }

        /// <summary>
        /// Executes the selected action on the data container.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute"))
                return;

            FluxDataContainer container = GetInputValue<FluxDataContainer>(inputs, "dataContainer", _dataContainer);
            
            if (container == null)
            {
                SetError(outputs, "Data Container input is null.");
                return;
            }

            try
            {
                bool success = false;
                switch (_action)
                {
                    case DataContainerAction.SaveToJson:
                        string json = container.SerializeToJson();
                        SetOutputValue(outputs, "json", json);
                        success = !string.IsNullOrEmpty(json);
                        break;

                    case DataContainerAction.LoadFromJson:
                        string jsonToLoad = GetInputValue<string>(inputs, "json");
                        if (string.IsNullOrEmpty(jsonToLoad))
                        {
                            SetError(outputs, "Input JSON Data is empty.");
                            return;
                        }
                        container.LoadFromJson(jsonToLoad);
                        success = true; // Assume success, LoadFromJson logs errors internally
                        break;
                    case DataContainerAction.Reset:
                        container.ResetReactiveProperties(); // Assuming a future method for this
                        success = true;
                        break;
                    case DataContainerAction.Validate:
                        bool isValid = container.ValidateData(out List<string> errors);
                        SetOutputValue(outputs, "isValid", isValid);
                        SetOutputValue(outputs, "errorMessages", errors);
                        
                        if (isValid)
                        {
                            SetSuccess(outputs);
                        }
                        else
                        {
                            SetError(outputs, "Data container failed global validation. Check 'Errors' output.");
                        }
                        success = isValid;
                        break;
                }

                if (success)
                {
                    SetSuccess(outputs);
                }
                else
                {
                    SetError(outputs, $"Action '{_action}' failed. Check console for details.");
                }
            }
            catch (Exception ex)
            {
                SetError(outputs, $"An exception occurred during '{_action}': {ex.Message}");
            }
        }

        private void SetSuccess(Dictionary<string, object> outputs)
        {
            SetOutputValue(outputs, "success", true);
            SetOutputValue(outputs, "onSuccess", null);
        }

        private void SetError(Dictionary<string, object> outputs, string error)
        {
            SetOutputValue(outputs, "success", false);
            SetOutputValue(outputs, "onFailure", null);
            Debug.LogError($"DataContainerNode: {error}", this);
        }
    }

    /// <summary>
    /// Defines the actions the DataContainerNode can perform.
    /// The ambiguous 'Save' and 'Load' actions have been removed in favor of explicit JSON operations.
    /// </summary>
    public enum DataContainerAction
    {
        SaveToJson,
        LoadFromJson,
        Validate,
        Reset
    }
}