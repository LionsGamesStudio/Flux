using System;

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
        [SerializeField] private string _valueTypeName; // Stores the AssemblyQualifiedName of the data type.

        public string Name => _name;
        public FluxPortType PortType => _portType;
        public FluxPortDirection Direction => _direction;
        public string ValueTypeName => _valueTypeName;

        // Public constructor
        public FluxNodePort(string name, FluxPortType portType, FluxPortDirection direction, Type valueType)
        {
            _name = name;
            _portType = portType;
            _direction = direction;
            _valueTypeName = valueType.AssemblyQualifiedName;
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