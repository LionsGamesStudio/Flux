using System;
using UnityEngine;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A serializable data-only class that represents a connection between two ports.
    /// This is part of the core data model.
    /// </summary>
    [Serializable]
    public class FluxNodeConnection
    {
        [SerializeField] private string _fromNodeId;
        [SerializeField] private string _fromPortName;
        [SerializeField] private string _toNodeId;
        [SerializeField] private string _toPortName;
        [SerializeField] private float _duration;

        public string FromNodeId => _fromNodeId;
        public string FromPortName => _fromPortName;
        public string ToNodeId => _toNodeId;
        public string ToPortName => _toPortName;
        public float Duration { get => _duration; set => _duration = value; }

        public FluxNodeConnection(string fromNodeId, string fromPortName, string toNodeId, string toPortName)
        {
            _fromNodeId = fromNodeId;
            _fromPortName = fromPortName;
            _toNodeId = toNodeId;
            _toPortName = toPortName;
            _duration = 0f;
        }
    }
}