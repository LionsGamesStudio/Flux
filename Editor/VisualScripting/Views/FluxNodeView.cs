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

        public FluxNodeView(FluxNodeBase node, FluxGraphView graphView) : base()
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

            CreateInputPorts(graphView.EdgeListener);
            CreateOutputPorts(graphView.EdgeListener);
            
            ApplyCategoryColor();
        }

        private void CreateInputPorts(IEdgeConnectorListener edgeListener)
        {
            foreach (var portData in Node.InputPorts)
            {
                var capacity = (portData.Capacity == PortCapacity.Multi) ? Port.Capacity.Multi : Port.Capacity.Single;
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                portView.userData = portData; // Store the port data for later use

                portView.AddManipulator(new EdgeConnector<Edge>(edgeListener));

                inputContainer.Add(portView);
            }
        }
        
        private void CreateOutputPorts(IEdgeConnectorListener listener)
        {
            foreach (var portData in Node.OutputPorts)
            {
                var capacity = (portData.Capacity == PortCapacity.Multi) ? Port.Capacity.Multi : Port.Capacity.Single;
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                portView.userData = portData; // Store the port data for later use

                portView.AddManipulator(new EdgeConnector<Edge>(listener));

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

        private void ApplyCategoryColor()
        {
            // Find the first theme asset in the project.
            var themeGuids = AssetDatabase.FindAssets("t:FluxGraphTheme");
            if (themeGuids.Length == 0) return; // No theme asset found.
            if(themeGuids.Length > 1)
            {
                Debug.LogWarning("Multiple FluxGraphTheme assets found. Using the first one.");
            }
            
            var themePath = AssetDatabase.GUIDToAssetPath(themeGuids[0]);
            var theme = AssetDatabase.LoadAssetAtPath<FluxGraphTheme>(themePath);
            if (theme == null) return;
            
            string category = "";
            if (Node is AttributedNodeWrapper wrapper && wrapper.NodeLogic != null)
            {
                var attr = wrapper.NodeLogic.GetType().GetCustomAttribute<FluxNodeAttribute>();
                if (attr != null)
                {
                    category = attr.Category;
                }
            }
            
            // Get the color from the theme and apply it to the node's title container.
            var headerColor = theme.GetColorForCategory(category);
            titleContainer.style.backgroundColor = headerColor;
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