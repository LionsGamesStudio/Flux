using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that directs the execution flow down one of many paths based on a matching value.
    /// It is the equivalent of a switch statement.
    /// </summary>
    [CreateAssetMenu(fileName = "SwitchNode", menuName = "Flux/Visual Scripting/Flow/Switch")]
    public class SwitchNode : FluxNodeBase
    {
        [SerializeField] private SwitchType _switchType = SwitchType.Int;
        [SerializeField] private List<SwitchCase> _cases = new List<SwitchCase>();
        [SerializeField] private bool _hasDefault = true;

        public override string NodeName => $"Switch ({_switchType})";
        public override string Category => "Flow";

        public SwitchType Type { get => _switchType; set { _switchType = value; RefreshPorts(); } }
        public List<SwitchCase> Cases { get => _cases; set { _cases = value; RefreshPorts(); } }
        public bool HasDefault { get => _hasDefault; set { _hasDefault = value; RefreshPorts(); } }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            
            string inputType = _switchType switch
            {
                SwitchType.Int => "int",
                SwitchType.String => "string",
                _ => "object"
            };
            AddInputPort("value", "Value", FluxPortType.Data, inputType, true);
            
            foreach (var switchCase in _cases)
            {
                // Ensure case values are unique to avoid port name collisions
                AddOutputPort($"case_{switchCase.Value.GetHashCode()}", $"▶ Case {switchCase.Value}", FluxPortType.Execution, "void", false);
            }
            
            if (_hasDefault)
            {
                AddOutputPort("default", "▶ Default", FluxPortType.Execution, "void", false);
            }
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            object inputValue = GetInputValue<object>(inputs, "value");
            bool caseMatched = false;

            foreach (var switchCase in _cases)
            {
                if (IsValueMatch(inputValue, switchCase.Value))
                {
                    SetOutputValue(outputs, $"case_{switchCase.Value.GetHashCode()}", null);
                    caseMatched = true;
                    break;
                }
            }

            if (!caseMatched && _hasDefault)
            {
                SetOutputValue(outputs, "default", null);
            }
        }

        private bool IsValueMatch(object inputValue, string caseValue)
        {
            try
            {
                return _switchType switch
                {
                    SwitchType.Int => Convert.ToInt32(inputValue) == Convert.ToInt32(caseValue),
                    SwitchType.String => Convert.ToString(inputValue).Equals(caseValue, StringComparison.Ordinal),
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }
    }

    public enum SwitchType { Int, String }
    [Serializable]
    public class SwitchCase { public string Value; }
}