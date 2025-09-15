using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// The abstract base class for any object that can exist as a node in a visual graph.
    /// It contains the core data required by the graph and the editor, such as position and ports.
    /// This is part of the core data model.
    /// </summary>
    public abstract class FluxNodeBase : ScriptableObject
    {
        [SerializeField] private string _nodeId = System.Guid.NewGuid().ToString();
        [SerializeField] private Vector2 _position;
        [SerializeField] protected List<FluxNodePort> _inputPorts = new List<FluxNodePort>();
        [SerializeField] protected List<FluxNodePort> _outputPorts = new List<FluxNodePort>();

        public string NodeId => _nodeId;
        public Vector2 Position { get => _position; set => _position = value; }
        public IReadOnlyList<FluxNodePort> InputPorts => _inputPorts;
        public IReadOnlyList<FluxNodePort> OutputPorts => _outputPorts;

        protected virtual void OnEnable() { }

        /// <summary>
        /// Adds a new input port to this node's data model.
        /// </summary>
        public void AddInputPort(string name, string displayName, FluxPortType portType, System.Type valueType, PortCapacity capacity = PortCapacity.Single)
        {
            _inputPorts.Add(new FluxNodePort(name, displayName, portType, FluxPortDirection.Input, valueType, capacity));
        }

        /// <summary>
        /// Adds a new output port to this node's data model.
        /// </summary>
        public void AddOutputPort(string name, string displayName, FluxPortType portType, System.Type valueType, PortCapacity capacity = PortCapacity.Multi)
        {
            _outputPorts.Add(new FluxNodePort(name, displayName, portType, FluxPortDirection.Output, valueType, capacity));
        }

        /// <summary>
        /// Clears all ports. Used when regenerating node structures.
        /// </summary>
        public void ClearPorts()
        {
            _inputPorts.Clear();
            _outputPorts.Clear();
        }
    }
}