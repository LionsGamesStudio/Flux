using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxNodeView : UnityEditor.Experimental.GraphView.Node 
    {
        private Dictionary<string, Label> _debugLabels = new Dictionary<string, Label>();

        public FluxNodeBase Node { get; }

        public FluxNodeView(FluxNodeBase node) : base()
        {
            this.Node = node;
            
            if (node is AttributedNodeWrapper wrapper && wrapper.NodeLogic != null)
            {
                var attr = wrapper.NodeLogic.GetType().GetCustomAttribute<FluxNodeAttribute>();
                if (attr != null)
                {
                    this.title = attr.DisplayName;
                }
            }
            else
            {
                this.title = node.name;
            }

            this.viewDataKey = node.NodeId; // This is used by Unity to save/load the layout

            style.left = node.Position.x;
            style.top = node.Position.y;

            CreateInputPorts();
            CreateOutputPorts();
        }

        private void CreateInputPorts()
        {
            foreach (var portData in Node.InputPorts)
            {
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                inputContainer.Add(portView);
            }
        }
        
        private void CreateOutputPorts()
        {
            foreach (var portData in Node.OutputPorts)
            {
                // Output ports can have multiple connections
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                outputContainer.Add(portView);
            }
        }

        /// <summary>
        /// Updates or creates debug labels next to the ports with live data from the executor.
        /// </summary>
        public void SetPortDebugValues(Dictionary<string, string> portValues)
        {
            foreach (var (portName, valueText) in portValues)
            {
                if (_debugLabels.TryGetValue(portName, out var label))
                {
                    // Update existing label
                    label.text = valueText;
                }
                else
                {
                    // Create new label for a port
                    var portView = GetPort(portName);
                    if (portView != null)
                    {
                        var newLabel = new Label(valueText);
                        newLabel.style.fontSize = 9;
                        newLabel.style.color = Color.gray;
                        // Add some spacing
                        if(portView.direction == Direction.Input) newLabel.style.marginRight = 5;
                        else newLabel.style.marginLeft = 5;

                        portView.parent.Insert(portView.parent.IndexOf(portView) + 1, newLabel);
                        _debugLabels[portName] = newLabel;
                    }
                }
            }
        }

        /// <summary>
        /// Removes all debug labels from this node. Called when exiting play mode.
        /// </summary>
        public void ClearPortDebugValues()
        {
            foreach (var label in _debugLabels.Values)
            {
                label.RemoveFromHierarchy();
            }
            _debugLabels.Clear();
        }

        private System.Type GetPortType(FluxNodePort portData)
        {
            return System.Type.GetType(portData.ValueTypeName) ?? typeof(object);
        }

        private Port GetPort(string name)
        {
            return (Port)inputContainer.Q(name) ?? (Port)outputContainer.Q(name);
        }
        
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            // Important: Update the data model when the view is moved.
            Undo.RecordObject(Node, "Move Node"); // Make this undo-able
            Node.Position = newPos.position;
            EditorUtility.SetDirty(Node);
        }
    }
}