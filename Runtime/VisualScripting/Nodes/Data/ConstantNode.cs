using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A pure data node that provides a constant value of a specified type.
    /// This node is executed by the graph runner whenever another node requires its output.
    /// </summary>
    [CreateAssetMenu(fileName = "ConstantNode", menuName = "Flux/Visual Scripting/Data/Constant")]
    public class ConstantNode : FluxNodeBase
    {
        [Tooltip("The data type of the constant value.")]
        [SerializeField] private ConstantType _constantType = ConstantType.Float;
        [SerializeField] private float _floatValue = 0f;
        [SerializeField] private int _intValue = 0;
        [SerializeField] private bool _boolValue = false;
        [SerializeField] private string _stringValue = "";
        [SerializeField] private Vector2 _vector2Value = Vector2.zero;
        [SerializeField] private Vector3 _vector3Value = Vector3.zero;

        public override string NodeName => $"Constant ({_constantType})";
        public override string Category => "Data";

        public override Type GetEditorViewType()
        {
            // This tells the FluxGraphView to use the specialized ConstantNodeView for this node.
            return typeof(ConstantNode);
        }

        public ConstantType Type 
        { 
            get => _constantType; 
            set 
            { 
                _constantType = value; 
                RefreshPorts(); // Refresh ports to update the output port's type
                NotifyChanged();
            } 
        }
        
        // Public properties for the custom editor (ConstantNodeView) to bind to.
        public float FloatValue { get => _floatValue; set { _floatValue = value; NotifyChanged(); } }
        public int IntValue { get => _intValue; set { _intValue = value; NotifyChanged(); } }
        public bool BoolValue { get => _boolValue; set { _boolValue = value; NotifyChanged(); } }
        public string StringValue { get => _stringValue; set { _stringValue = value; NotifyChanged(); } }
        public Vector2 Vector2Value { get => _vector2Value; set { _vector2Value = value; NotifyChanged(); } }
        public Vector3 Vector3Value { get => _vector3Value; set { _vector3Value = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            // This node only has one output port, which changes type based on the inspector selection.
            string valueType = _constantType switch
            {
                ConstantType.Float => "float",
                ConstantType.Int => "int",
                ConstantType.Bool => "bool",
                ConstantType.String => "string",
                ConstantType.Vector2 => "Vector2",
                ConstantType.Vector3 => "Vector3",
                _ => "object"
            };

            AddOutputPort("value", "Value", FluxPortType.Data, valueType);
        }

        /// <summary>
        /// This method is called by the graph executor when a connected node needs this constant's value.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            object value = _constantType switch
            {
                ConstantType.Float => _floatValue,
                ConstantType.Int => _intValue,
                ConstantType.Bool => _boolValue,
                ConstantType.String => _stringValue,
                ConstantType.Vector2 => _vector2Value,
                ConstantType.Vector3 => _vector3Value,
                _ => null
            };

            SetOutputValue(outputs, "value", value);
        }
    }

    /// <summary>
    /// Defines the supported constant types for the ConstantNode.
    /// </summary>
    public enum ConstantType
    {
        Float,
        Int,
        Bool,
        String,
        Vector2,
        Vector3
    }
}