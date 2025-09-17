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
        [SerializeField] private string _displayName;
        [SerializeField] private FluxPortType _portType;
        [SerializeField] private FluxPortDirection _direction;
        [SerializeField] private string _valueTypeName; // Stores the AssemblyQualifiedName of the data type.
        [SerializeField] private PortCapacity _capacity;
        [SerializeField] private float _probabilityWeight = 1.0f;

        public string Name => _name;
        public string DisplayName => _displayName;
        public FluxPortType PortType => _portType;
        public FluxPortDirection Direction => _direction;
        public string ValueTypeName => _valueTypeName;
        public PortCapacity Capacity => _capacity;
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
            
            // Basic Rule: Directions must be opposite.
            if (this.Direction == otherPort.Direction) return false;

            // --- TYPE-SPECIFIC RULES ---
            
            // Get the port that is the SOURCE of the connection (Output) and the one that is the SINK (Input).
            var sourcePort = (this.Direction == FluxPortDirection.Output) ? this : otherPort;
            var sinkPort = (this.Direction == FluxPortDirection.Input) ? this : otherPort;

            switch (sourcePort.PortType)
            {
                case FluxPortType.Execution:
                    // An Execution source can ONLY connect to an Execution sink.
                    return sinkPort.PortType == FluxPortType.Execution;

                case FluxPortType.Data:
                    // A Data source can connect to a Data sink OR a Property sink.
                    if (sinkPort.PortType == FluxPortType.Data)
                    {
                        return IsValueTypeCompatible(sourcePort.ValueTypeName, sinkPort.ValueTypeName);
                    }
                    if (sinkPort.PortType == FluxPortType.Property)
                    {
                        // You can connect a value to a property port (e.g., to set its initial value).
                        // We need to check if the data type is compatible with the property's generic type.
                        return IsValueTypeCompatible(sourcePort.ValueTypeName, sinkPort.ValueTypeName);
                    }
                    return false;

                case FluxPortType.Event:
                    // An Event source can ONLY connect to an Execution sink. This triggers a flow.
                    return sinkPort.PortType == FluxPortType.Execution;

                case FluxPortType.Property:
                    // A Property source can connect to a Property sink OR a Data sink.
                    if (sinkPort.PortType == FluxPortType.Property)
                    {
                        // Property-to-Property connection (passing the live link).
                        return IsValueTypeCompatible(sourcePort.ValueTypeName, sinkPort.ValueTypeName);
                    }
                    if (sinkPort.PortType == FluxPortType.Data)
                    {
                        // You can get the value from a property port and pass it to a data port.
                        // The property's generic type must be compatible with the data port's type.
                        return IsValueTypeCompatible(sourcePort.ValueTypeName, sinkPort.ValueTypeName);
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if a value from one type can be assigned to a variable of another type.
        /// </summary>
        private bool IsValueTypeCompatible(string fromTypeName, string toTypeName)
        {
            // Rule 0: Exact match is always valid.
            if (fromTypeName == toTypeName) return true;
            
            Type fromType = Type.GetType(fromTypeName);
            Type toType = Type.GetType(toTypeName);
            
            // This can happen if an assembly is not loaded yet, so it's a safe check.
            if (fromType == null || toType == null) return false;

            // Rule 1 (Sink is Object): Anything can be connected TO an 'object' port.
            // This is safe because any type can be implicitly cast to object.
            if (toType == typeof(object)) return true;
            
            // Rule 2 (Source is Object): An 'object' output can connect TO ANYTHING.
            // This is an optimistic rule for nodes like 'Add' that use dynamic types.
            // We trust the user that the runtime value will be convertible.
            // A runtime error will occur if the conversion fails, which is acceptable.
            if (fromType == typeof(object)) return true;
            
            // Rule 3 (Inheritance): A derived class can be connected to a base class port.
            // (e.g., a specific FluxUIComponent to a generic Component port).
            if (toType.IsAssignableFrom(fromType)) return true;
            
            // Rule 4 (Explicit Conversion): Check if an explicit conversion is possible.
            // This handles cases like int -> float, float -> double, etc.
            try
            {
                // We use FormatterServices to get an uninitialized object, which is faster
                // and doesn't require a parameterless constructor.
                var dummyValue = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(fromType);
                System.Convert.ChangeType(dummyValue, toType);
                return true; // If no exception is thrown, the conversion is possible.
            }
            catch
            {
                // The conversion is not supported by System.Convert.
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