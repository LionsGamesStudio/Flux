using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
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
        
        // This non-serialized reference to the parent graph is crucial for nodes
        // that need to query their connections (like ForEach).
        [NonSerialized]
        private FluxVisualGraph _parentGraph;

        public INode NodeLogic => _nodeLogic;
        public FluxVisualGraph ParentGraph { get => _parentGraph; set => _parentGraph = value; }
        
        /// <summary>
        /// Finds the first node connected to a specific output port of this wrapper.
        /// This is a vital helper for flow-control nodes like ForEach and Branch.
        /// </summary>
        public FluxNodeBase GetConnectedNode(string portName)
        {
            if (_parentGraph == null) return null;

            var connection = _parentGraph.Connections.FirstOrDefault(c => c.FromNodeId == NodeId && c.FromPortName == portName);
            if (connection == null) return null;

            return _parentGraph.Nodes.FirstOrDefault(n => n.NodeId == connection.ToNodeId);
        }

        #if UNITY_EDITOR
        /// <summary>
        /// (Editor-only) Initializes the wrapper with a specific type of INode logic.
        /// </summary>
        public void Initialize(Type nodeLogicType, FluxVisualGraph graph)
        {
            if (nodeLogicType == null || !typeof(INode).IsAssignableFrom(nodeLogicType))
            {
                Debug.LogError($"[AttributedNodeWrapper] Type '{nodeLogicType?.Name}' is not a valid INode.");
                return;
            }

            _parentGraph = graph; // Set the parent graph reference
            _nodeLogic = (INode)Activator.CreateInstance(nodeLogicType);
            
            GeneratePortsFromLogic();

            if (_nodeLogic is IPortPostConfiguration postConfigurator)
            {
                postConfigurator.PostConfigurePorts(this);
            }
        }
        
        /// <summary>
        /// Scans the hosted INode logic for both static [Port] attributes on fields
        /// and dynamic port definitions from the IPortConfiguration interface, then
        /// creates the corresponding FluxNodePort entries in this wrapper's data model.
        /// </summary>
        private void GeneratePortsFromLogic()
        {
            if (_nodeLogic == null) return;
            
            ClearPorts(); // Start with a clean slate
            
            var logicType = _nodeLogic.GetType();

            // --- 1. Add STATIC ports defined by [Port] attributes on fields ---
            var fields = logicType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var portAttr = field.GetCustomAttribute<PortAttribute>();
                if (portAttr == null) continue;

                string portName = field.Name;
                string displayName = portAttr.DisplayName ?? GenerateDisplayName(portName);
                Type valueType = field.FieldType;
                var capacity = portAttr.Capacity;
                
                if (portAttr.PortType == FluxPortType.Execution)
                {
                    valueType = typeof(ExecutionPin);
                }

                if (portAttr.Direction == FluxPortDirection.Input)
                {
                    AddInputPort(portName, displayName, portAttr.PortType, valueType, capacity);
                }
                else // Output
                {
                    AddOutputPort(portName, displayName, portAttr.PortType, valueType, capacity);
                }
            }
            
            // --- 2. Add DYNAMIC ports if the node implements the configuration interface ---
            if (_nodeLogic is IPortConfiguration configurator)
            {
                var dynamicPorts = configurator.GetDynamicPorts();
                if (dynamicPorts == null) return;

                foreach (var portDef in dynamicPorts)
                {
                    // The ValueTypeName from the definition is an AssemblyQualifiedName. We need to convert it back to a Type.
                    var type = Type.GetType(portDef.ValueTypeName) ?? typeof(object);
                    
                    if (portDef.Direction == FluxPortDirection.Input)
                    {
                        AddInputPort(portDef.PortName, portDef.PortName, portDef.PortType, type, portDef.Capacity);
                    }
                    else // Output
                    {
                        AddOutputPort(portDef.PortName, portDef.PortName, portDef.PortType, type, portDef.Capacity);
                    }
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// (Editor-only) Completely rebuilds the node's ports based on its current logic and configuration.
        /// This is essential for dynamic nodes like GraphInput/Output.
        /// </summary>
        public void RebuildPorts()
        {
            if (_nodeLogic == null) return;
            
            // This sequence ensures everything is regenerated correctly.
            GeneratePortsFromLogic();

            if (_nodeLogic is IPortPostConfiguration postConfigurator)
            {
                postConfigurator.PostConfigurePorts(this);
            }
            
            // Mark the asset as dirty so the changes to the port lists are saved.
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public FluxNodePort FindPort(string name)
        {
            return (FluxNodePort)_inputPorts.FirstOrDefault(p => p.Name == name) ?? 
                   (FluxNodePort)_outputPorts.FirstOrDefault(p => p.Name == name);
        }
        
        private string GenerateDisplayName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return fieldName;
            
            // This is a more robust way to "nicify" camelCase and PascalCase names.
            return Regex.Replace(fieldName, "(\\B[A-Z])", " $1");
        }
        #endif
    }
    
    /// <summary>
    /// A simple, empty struct used as a placeholder type for Execution ports.
    /// </summary>
    public struct ExecutionPin { }
}