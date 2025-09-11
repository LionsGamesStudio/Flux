using System;
using System.Linq;
using UnityEngine;

namespace FluxFramework.VisualScripting.Graphs
{
    /// <summary>
    /// Represents a connection between two nodes in a visual scripting graph
    /// </summary>
    [Serializable]
    public class FluxNodeConnection
    {
        [SerializeField] private FluxNodeBase _fromNode;
        [SerializeField] private string _fromPort;
        [SerializeField] private FluxNodeBase _toNode;
        [SerializeField] private string _toPort;

        /// <summary>
        /// Source node of the connection
        /// </summary>
        public FluxNodeBase FromNode => _fromNode;

        /// <summary>
        /// Source port name
        /// </summary>
        public string FromPort => _fromPort;

        /// <summary>
        /// Target node of the connection
        /// </summary>
        public FluxNodeBase ToNode => _toNode;

        /// <summary>
        /// Target port name
        /// </summary>
        public string ToPort => _toPort;

        public FluxNodeConnection(FluxNodeBase fromNode, string fromPort, FluxNodeBase toNode, string toPort)
        {
            _fromNode = fromNode;
            _fromPort = fromPort;
            _toNode = toNode;
            _toPort = toPort;
        }

        /// <summary>
        /// Check if this connection is valid
        /// </summary>
        public bool IsValid()
        {
            if (_fromNode == null || _toNode == null) return false;
            if (string.IsNullOrEmpty(_fromPort) || string.IsNullOrEmpty(_toPort)) return false;

            var fromPortObj = _fromNode.OutputPorts.FirstOrDefault(p => p.Name == _fromPort);
            var toPortObj = _toNode.InputPorts.FirstOrDefault(p => p.Name == _toPort);

            return fromPortObj != null && toPortObj != null && fromPortObj.CanConnectTo(toPortObj);
        }

        public override string ToString()
        {
            return $"{_fromNode?.NodeName}.{_fromPort} -> {_toNode?.NodeName}.{_toPort}";
        }
    }
}
