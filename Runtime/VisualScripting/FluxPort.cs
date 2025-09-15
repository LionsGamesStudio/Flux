using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A serializable data-only class that represents a single connection point on a node.
    /// This is part of the core data model.
    /// </summary>
    [Serializable]
    public class FluxNodePort
    {
        [SerializeField] private string _name;
        [SerializeField] private FluxPortType _portType;
        [SerializeField] private FluxPortDirection _direction;
        [SerializeField] private PortCapacity _capacity;
        [SerializeField] private string _valueTypeName; // Stores the AssemblyQualifiedName of the data type.
        [SerializeField] private string _displayName;
        [SerializeField] private float _probabilityWeight = 1.0f;

        public string Name => _name;
        public FluxPortType PortType => _portType;
        public FluxPortDirection Direction => _direction;
        public PortCapacity Capacity => _capacity;
        public string ValueTypeName => _valueTypeName;
        public string DisplayName => _displayName;
        public float ProbabilityWeight { get => _probabilityWeight; set => _probabilityWeight = value; }

        // Public constructor
        internal FluxNodePort(string name, string displayName, FluxPortType portType, FluxPortDirection direction, Type valueType, PortCapacity capacity)
        {
            _name = name;
            _displayName = displayName;
            _portType = portType;
            _direction = direction;
            _capacity = capacity;
            _valueTypeName = valueType.AssemblyQualifiedName;
            _probabilityWeight = 1.0f;
        }

        /// <summary>
        /// Checks if this port can legally connect to another port based on a set of rules.
        /// </summary>
        public bool CanConnectTo(FluxNodePort otherPort)
        {
            if (otherPort == null) return false;

            // Rule 1: Cannot connect to a port on the same node.
            if (ReferenceEquals(this, otherPort)) return false;
            
            // Rule 2: Directions must be opposite (Input to Output, or Output to Input).
            if (this.Direction == otherPort.Direction) return false;

            // Rule 3: Port types must be the same (Execution to Execution, Data to Data).
            if (this.PortType != otherPort.PortType) return false;
            
            // Rule 4 (for Data ports): The value types must be compatible.
            if (this.PortType == FluxPortType.Data)
            {
                var fromType = (this.Direction == FluxPortDirection.Output) ? this.ValueTypeName : otherPort.ValueTypeName;
                var toType = (this.Direction == FluxPortDirection.Input) ? this.ValueTypeName : otherPort.ValueTypeName;
                return IsValueTypeCompatible(fromType, toType);
            }

            // If all checks pass, the connection is valid.
            return true;
        }

        /// <summary>
        /// Checks if a value from one type can be assigned to a variable of another type.
        /// </summary>
        private bool IsValueTypeCompatible(string fromTypeName, string toTypeName)
        {
            if (fromTypeName == toTypeName) return true;
            
            Type fromType = Type.GetType(fromTypeName);
            Type toType = Type.GetType(toTypeName);
            
            if (fromType == null || toType == null) return false; // Should not happen

            // Rule A: Anything can be connected to an 'object' port.
            if (toType == typeof(object)) return true;
            
            // Rule B: A derived class can be connected to a base class port (e.g., a specific FluxUIComponent to a generic FluxUIComponent).
            if (toType.IsAssignableFrom(fromType)) return true;
            
            // Rule C: Check for common numeric conversions (e.g., int to float).
            try
            {
                var dummyValue = Activator.CreateInstance(fromType);
                System.Convert.ChangeType(dummyValue, toType);
                return true; // If no exception is thrown, the conversion is possible.
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Defines the fundamental purpose of a port in the visual scripting system.
    /// This dictates its color, shape, and connection rules.
    /// </summary>
    [Serializable]
    public enum FluxPortType
    {
        /// <summary>
        /// Carries a signal to dictate the order of operations. Does not carry data.
        /// </summary>
        Execution,
        
        /// <summary>
        /// Carries a value or an object reference.
        /// </summary>
        Data,
        
        /// <summary>
        /// Represents a connection to a specific event system.
        /// </summary>
        Event,
        
        /// <summary>
        /// Represents a live, persistent link to a Reactive Property.
        /// </summary>
        Property
    }

    /// <summary>
    /// Defines the flow of information for a port.
    /// </summary>
    [Serializable]
    public enum FluxPortDirection
    {
        /// <summary>
        /// The port receives data or an execution signal.
        /// </summary>
        Input,
        
        /// <summary>
        /// The port provides data or an execution signal.
        /// </summary>
        Output
    }
}