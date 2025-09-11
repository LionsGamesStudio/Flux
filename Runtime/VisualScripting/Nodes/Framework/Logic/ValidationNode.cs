using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Validation;
using FluxFramework.Attributes;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that performs various validation checks on an input value.
    /// It uses the framework's central validation logic for consistency.
    /// </summary>
    [CreateAssetMenu(fileName = "ValidationNode", menuName = "Flux/Visual Scripting/Framework/Logic/Validation")]
    public class ValidationNode : FluxNodeBase
    {
        [SerializeField] private ValidationType _validationType = ValidationType.Range;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 100f;
        [SerializeField] private int _minLength = 0;
        [SerializeField] private int _maxLength = 255;
        [SerializeField] private string _pattern = "";

        public override string NodeName => $"Validate ({_validationType})";
        public override string Category => "Framework/Logic";

        public ValidationType ValidationType 
        { 
            get => _validationType; 
            set { _validationType = value; RefreshPorts(); } 
        }

        protected override void InitializePorts()
        {
            // Execution input is always present to trigger the validation.
            AddInputPort("execute", "Execute", FluxPortType.Execution, "void", true);
            
            // Data inputs
            AddInputPort("value", "Value", FluxPortType.Data, "object", true);
            
            // Dynamic ports based on validation type
            switch (_validationType)
            {
                case ValidationType.Range:
                    AddInputPort("min", "Min", FluxPortType.Data, "float", false, _minValue);
                    AddInputPort("max", "Max", FluxPortType.Data, "float", false, _maxValue);
                    break;
                case ValidationType.StringLength:
                    AddInputPort("minLength", "Min Length", FluxPortType.Data, "int", false, _minLength);
                    AddInputPort("maxLength", "Max Length", FluxPortType.Data, "int", false, _maxLength);
                    break;
                case ValidationType.Pattern:
                    AddInputPort("pattern", "Regex Pattern", FluxPortType.Data, "string", false, _pattern);
                    break;
            }
            
            // Execution outputs
            AddOutputPort("onValid", "On Valid", FluxPortType.Execution, "void", false);
            AddOutputPort("onInvalid", "On Invalid", FluxPortType.Execution, "void", false);
            
            // Data outputs
            AddOutputPort("isValid", "Is Valid", FluxPortType.Data, "bool", false);
            AddOutputPort("errorMessages", "Error Messages", FluxPortType.Data, "string[]", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            object value = GetInputValue<object>(inputs, "value");
            
            // Perform the validation using our new central logic
            var result = ValidateValue(value, inputs);
            
            SetOutputValue(outputs, "isValid", result.IsValid);
            SetOutputValue(outputs, "errorMessages", result.ErrorMessages);
            
            if (result.IsValid)
            {
                SetOutputValue(outputs, "onValid", null);
            }
            else
            {
                SetOutputValue(outputs, "onInvalid", null);
            }
        }

        /// <summary>
        /// This method now instantiates and uses the central IValidator classes
        /// instead of containing its own validation logic.
        /// </summary>
        private Core.ValidationResult ValidateValue(object value, Dictionary<string, object> inputs)
        {
            try
            {
                switch (_validationType)
                {
                    case ValidationType.NotNull:
                        return (value != null) ? Core.ValidationResult.Success : Core.ValidationResult.Failure("Value cannot be null.");

                    case ValidationType.NotEmpty:
                        if (value == null) return Core.ValidationResult.Failure("Value cannot be null.");
                        if (value is string s && string.IsNullOrEmpty(s)) return Core.ValidationResult.Failure("String cannot be empty.");
                        // Could add checks for collections here in the future.
                        return Core.ValidationResult.Success;
                        
                    case ValidationType.Range:
                        float min = GetInputValue<float>(inputs, "min", _minValue);
                        float max = GetInputValue<float>(inputs, "max", _maxValue);
                        // Handle different numeric types by converting to double for comparison
                        if (value is int i) return new RangeValidator<int>((int)min, (int)max).Validate(i);
                        if (value is float f) return new RangeValidator<float>(min, max).Validate(f);
                        return Core.ValidationResult.Failure("Input value is not a supported number (int or float).");

                    case ValidationType.StringLength:
                        int minLength = GetInputValue<int>(inputs, "minLength", _minLength);
                        int maxLength = GetInputValue<int>(inputs, "maxLength", _maxLength);
                        if (value is string str)
                        {
                            // We create a temporary attribute to configure the validator, which is a clean pattern.
                            var attr = new FluxStringLengthAttribute(minLength, maxLength);
                            return new StringLengthValidator(attr).Validate(str);
                        }
                        return Core.ValidationResult.Failure("Input value is not a string.");

                    case ValidationType.Pattern:
                        string pattern = GetInputValue<string>(inputs, "pattern", _pattern);
                        if (string.IsNullOrEmpty(pattern)) return Core.ValidationResult.Success; // No pattern means always valid
                        if (value is string strToMatch)
                        {
                             return Regex.IsMatch(strToMatch, pattern) ? 
                                Core.ValidationResult.Success : 
                                Core.ValidationResult.Failure($"Value '{strToMatch}' does not match the pattern.");
                        }
                        return Core.ValidationResult.Failure("Input value is not a string.");
                        
                    default:
                        return Core.ValidationResult.Failure($"Unknown validation type: {_validationType}");
                }
            }
            catch (Exception ex)
            {
                return Core.ValidationResult.Failure($"Validation threw an exception: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Defines the types of validation the ValidationNode can perform.
    /// </summary>
    public enum ValidationType
    {
        NotNull,
        NotEmpty,
        Range,
        StringLength,
        Pattern
    }
}