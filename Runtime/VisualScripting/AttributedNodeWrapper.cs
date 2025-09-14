using System;
using System.Reflection;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// A concrete implementation of FluxNodeBase that acts as a "shell" or "host"
    /// for a pure C# class that implements the INode interface.
    /// It uses reflection to generate its ports based on the attributes of the hosted INode instance.
    /// </summary>
    [Serializable]
    public class AttributedNodeWrapper : FluxNodeBase
    {
        [SerializeReference]
        private INode _nodeLogic;
        
        public INode NodeLogic => _nodeLogic;

        #if UNITY_EDITOR
        /// <summary>
        /// (Editor-only) Initializes the wrapper with a specific type of INode logic.
        /// This creates an instance of the logic and populates the wrapper's ports.
        /// </summary>
        public void Initialize(Type nodeLogicType)
        {
            if (nodeLogicType == null || !typeof(INode).IsAssignableFrom(nodeLogicType))
            {
                Debug.LogError($"[AttributedNodeWrapper] Type '{nodeLogicType?.Name}' is not a valid INode.");
                return;
            }

            _nodeLogic = (INode)Activator.CreateInstance(nodeLogicType);
            
            // Generate the visual ports from the logic's attributes
            GeneratePortsFromLogic();
        }
        
        /// <summary>
        /// Scans the hosted INode logic for [Port] attributes and creates the
        /// corresponding FluxNodePort entries in this wrapper's data model.
        /// </summary>
        private void GeneratePortsFromLogic()
        {
            if (_nodeLogic == null) return;
            
            ClearPorts(); // Start with a clean slate
            
            var logicType = _nodeLogic.GetType();
            var fields = logicType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var portAttr = field.GetCustomAttribute<PortAttribute>();
                if (portAttr == null) continue;

                string portName = field.Name;
                Type valueType = field.FieldType;
                
                // For Execution ports, we need a way to represent their 'void' type.
                // We'll create a dummy type for this.
                if (portAttr.PortType == FluxPortType.Execution)
                {
                    valueType = typeof(ExecutionPin);
                }

                if (portAttr.Direction == FluxPortDirection.Input)
                {
                    AddInputPort(portName, portAttr.PortType, valueType);
                }
                else // Output
                {
                    AddOutputPort(portName, portAttr.PortType, valueType);
                }
            }
        }
        #endif
    }
    
    /// <summary>
    /// A simple, empty struct used as a placeholder type for Execution ports,
    /// since they don't carry any data.
    /// </summary>
    public struct ExecutionPin { }
}