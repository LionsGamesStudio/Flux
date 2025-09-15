using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// A serializable data structure for defining a custom port in the inspector,
    /// used by GraphInputNode and GraphOutputNode.
    /// </summary>
    [Serializable]
    public class CustomPortDefinition
    {
        [Tooltip("The internal name of the port, used for connections.")]
        public string PortName = "NewPort";

        [Tooltip("The direction of the port (Input or Output).")]
        public FluxPortDirection Direction = FluxPortDirection.Output;

        [Tooltip("The type of the port.")]
        public FluxPortType PortType = FluxPortType.Data;

        [Tooltip("The capacity of the port (Single or Multi).")]
        public PortCapacity Capacity = PortCapacity.Single;

        [Tooltip("The data type of the port.")]
        public string ValueTypeName = typeof(float).AssemblyQualifiedName;
    }
}