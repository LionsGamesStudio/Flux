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
        [SerializeField] private string _valueTypeName; // Stores the AssemblyQualifiedName of the data type.
        [SerializeField] private string _displayName;
        [SerializeField] private float _probabilityWeight = 1.0f;

        public string Name => _name;
        public FluxPortType PortType => _portType;
        public FluxPortDirection Direction => _direction;
        public string ValueTypeName => _valueTypeName;
        public string DisplayName => _displayName;
        public float ProbabilityWeight { get => _probabilityWeight; set => _probabilityWeight = value; }

        // Public constructor
        internal FluxNodePort(string name, string displayName, FluxPortType portType, FluxPortDirection direction, Type valueType)
        {
            _name = name;
            _displayName = displayName;
            _portType = portType;
            _direction = direction;
            _valueTypeName = valueType.AssemblyQualifiedName;
            _probabilityWeight = 1.0f;
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