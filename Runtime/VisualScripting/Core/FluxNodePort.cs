using System;
using UnityEngine;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// Represents a connection point on a visual scripting node
    /// </summary>
    [Serializable]
    public class FluxNodePort
    {
        [SerializeField] private string _name;
        [SerializeField] private string _displayName;
        [SerializeField] private FluxPortType _portType;
        [SerializeField] private FluxPortDirection _direction;
        [SerializeField] private string _valueType;
        [SerializeField] private bool _isRequired;
        [SerializeField] private object _defaultValue;
        [SerializeField] private string _tooltip;

        /// <summary>
        /// Internal name of the port
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Display name shown in the editor
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Type of port (Data, Execution, etc.)
        /// </summary>
        public FluxPortType PortType => _portType;

        /// <summary>
        /// Direction of the port (Input or Output)
        /// </summary>
        public FluxPortDirection Direction => _direction;

        /// <summary>
        /// Type name of the value this port handles
        /// </summary>
        public string ValueType => _valueType;

        /// <summary>
        /// Whether this port must be connected
        /// </summary>
        public bool IsRequired => _isRequired;

        /// <summary>
        /// Default value when port is not connected
        /// </summary>
        public object DefaultValue => _defaultValue;

        /// <summary>
        /// Tooltip description for the port
        /// </summary>
        public string Tooltip => _tooltip;

        /// <summary>
        /// Whether this port is currently connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Connected port (if any)
        /// </summary>
        public FluxNodePort ConnectedPort { get; set; }

        public FluxNodePort(string name, string displayName, FluxPortType portType, 
                           FluxPortDirection direction, string valueType, 
                           bool isRequired = false, object defaultValue = null, string tooltip = null)
        {
            _name = name;
            _displayName = displayName;
            _portType = portType;
            _direction = direction;
            _valueType = valueType;
            _isRequired = isRequired;
            _defaultValue = defaultValue;
            _tooltip = tooltip;
        }

        /// <summary>
        /// Check if this port can connect to another port
        /// </summary>
        /// <param name="otherPort">Port to check compatibility with</param>
        /// <returns>True if ports can be connected</returns>
        public bool CanConnectTo(FluxNodePort otherPort)
        {
            if (otherPort == null) return false;
            if (Direction == otherPort.Direction) return false;
            if (PortType != otherPort.PortType) return false;
            
            // Check type compatibility
            return IsTypeCompatible(ValueType, otherPort.ValueType);
        }

        /// <summary>
        /// Check if two types are compatible for connection
        /// </summary>
        private bool IsTypeCompatible(string type1, string type2)
        {
            if (type1 == type2) return true;
            if (type1 == "object" || type2 == "object") return true;
            
            // Allow numeric conversions
            var numericTypes = new[] { "int", "float", "double" };
            if (Array.IndexOf(numericTypes, type1) >= 0 && Array.IndexOf(numericTypes, type2) >= 0)
                return true;

            // Allow bool conversions (useful for conditions)
            if ((type1 == "bool" && IsConvertibleToBool(type2)) || 
                (type2 == "bool" && IsConvertibleToBool(type1)))
                return true;

            // Allow string conversions (most things can become strings)
            if (type1 == "string" || type2 == "string")
                return true;

            return false;
        }

        private bool IsConvertibleToBool(string type)
        {
            // Numbers can be converted to bool (0 = false, non-zero = true)
            var convertibleTypes = new[] { "int", "float", "double", "bool" };
            return Array.IndexOf(convertibleTypes, type) >= 0;
        }
    }

    /// <summary>
    /// Types of ports available in the visual scripting system
    /// </summary>
    public enum FluxPortType
    {
        Data,           // Data flow port
        Execution,      // Execution flow port
        Event,          // Event trigger port
        Property        // Reactive property port
    }

    /// <summary>
    /// Direction of port data flow
    /// </summary>
    public enum FluxPortDirection
    {
        Input,
        Output
    }
}
