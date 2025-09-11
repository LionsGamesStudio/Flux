using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that generates a random value each time it is executed.
    /// </summary>
    [CreateAssetMenu(fileName = "RandomNode", menuName = "Flux/Visual Scripting/Math/Random")]
    public class RandomNode : FluxNodeBase
    {
        [SerializeField] private RandomType _randomType = RandomType.Float;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 1f;

        public override string NodeName => $"Random ({_randomType})";
        public override string Category => "Math";

        public RandomType Type 
        { 
            get => _randomType; 
            set { _randomType = value; RefreshPorts(); } 
        }
        public float MinValue { get => _minValue; set { _minValue = value; NotifyChanged(); } }
        public float MaxValue { get => _maxValue; set { _maxValue = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            // Execution input is required to generate a *new* random value.
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            
            // Only show Min/Max ports when they are relevant.
            if (_randomType == RandomType.Float || _randomType == RandomType.Int)
            {
                AddInputPort("min", "Min", FluxPortType.Data, "float", false, _minValue);
                AddInputPort("max", "Max", FluxPortType.Data, "float", false, _maxValue);
            }
            
            AddOutputPort("onGenerated", "▶ Out", FluxPortType.Execution, "void", false);
            
            string outputType = _randomType switch
            {
                RandomType.Int => "int",
                RandomType.Bool => "bool",
                _ => "float" // Float and Range01 both output float
            };
            AddOutputPort("value", "Value", FluxPortType.Data, outputType, false);
        }

        /// <summary>
        /// Executes the random value generation.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            float min = GetInputValue<float>(inputs, "min", _minValue);
            float max = GetInputValue<float>(inputs, "max", _maxValue);

            object randomValue = _randomType switch
            {
                RandomType.Float => Random.Range(min, max),
                RandomType.Int => Random.Range(Mathf.RoundToInt(min), Mathf.RoundToInt(max)), // Unity's int Range is exclusive for the max value
                RandomType.Bool => Random.value > 0.5f,
                RandomType.Range01 => Random.value,
                _ => 0f
            };

            SetOutputValue(outputs, "value", randomValue);
            SetOutputValue(outputs, "onGenerated", null);
        }
    }

    public enum RandomType
    {
        Float, // A random float within a specified range
        Int,   // A random integer within a specified range
        Bool,  // A 50/50 chance of being true or false
        Range01 // A random float between 0.0 and 1.0 (same as Random.value)
    }
}