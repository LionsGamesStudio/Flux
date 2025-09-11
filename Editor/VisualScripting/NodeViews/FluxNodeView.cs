using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// The visual representation of a FluxNodeBase in the graph editor.
    /// It handles drawing the node, its ports, and its custom content.
    /// </summary>
    public class FluxNodeView : Node
    {
        public FluxNodeBase Node { get; }
        public FluxVisualGraph Graph { get; }

        /// <summary>
        /// The constructor now requires both the node data and the parent graph.
        /// </summary>
        public FluxNodeView(FluxVisualGraph graph, FluxNodeBase node) : base()
        {
            Graph = graph;
            Node = node;
            title = node.NodeName;
            viewDataKey = node.NodeId; // Used by GraphView to save/load layout.

            style.left = node.Position.x;
            style.top = node.Position.y;

            Node.OnChanged += OnNodeDataChanged;
            
            CreateInputPorts();
            CreateOutputPorts();
            SetNodeColor();
            CreateNodeContent();
            
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnNodeDataChanged(IFluxNode node)
        {
            title = node.NodeName;
            RefreshCustomNameDisplay();
        }
        
        private void CreateInputPorts()
        {
            foreach (var port in Node.InputPorts)
            {
                var graphPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, GetPortSystemType(port.ValueType));
                
                // Use port.name for the internal identifier and port.portName for the visible label.
                graphPort.name = port.Name; // Internal ID for connections
                graphPort.portName = port.DisplayName; // Visible label

                graphPort.userData = port;
                graphPort.tooltip = port.Tooltip;

                SetPortColor(graphPort, port);
                inputContainer.Add(graphPort);
            }
        }

        private void CreateOutputPorts()
        {
            foreach (var port in Node.OutputPorts)
            {
                var graphPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, GetPortSystemType(port.ValueType));
                
                graphPort.name = port.Name; // Internal ID
                graphPort.portName = port.DisplayName; // Visible label
                
                graphPort.userData = port;
                graphPort.tooltip = port.Tooltip;
                
                SetPortColor(graphPort, port);
                outputContainer.Add(graphPort);
            }
        }

        // This method can be overridden in specialized node views (e.g., ConstantNodeView)
        protected virtual void CreateNodeContent()
        {
            RefreshCustomNameDisplay();
        }

        private void RefreshCustomNameDisplay()
        {
            var contentContainer = mainContainer.Q("content");
            if (contentContainer == null)
            {
                contentContainer = new VisualElement { name = "content", style = { paddingTop = 4, paddingBottom = 4, paddingLeft = 8, paddingRight = 8 }};
                mainContainer.Insert(1, contentContainer);
            }

            var existingLabel = contentContainer.Q<Label>("custom-name-label");
            existingLabel?.RemoveFromHierarchy();
            
            var hasCustomName = !string.IsNullOrEmpty(Node.CustomDisplayName);
            var displayText = hasCustomName ? Node.CustomDisplayName : "Double-click to rename";
            
            var customNameLabel = new Label(displayText) { 
                name = "custom-name-label", 
                style = { 
                    fontSize = 12, 
                    unityFontStyleAndWeight = hasCustomName ? FontStyle.Bold : FontStyle.Italic, 
                    alignSelf = Align.Center 
                }
            };
            
            customNameLabel.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    StartCustomNameEdit(customNameLabel);
                    evt.StopPropagation();
                }
            });
            
            contentContainer.Insert(0, customNameLabel);
        }

        private void StartCustomNameEdit(Label customNameLabel)
        {
            var contentContainer = mainContainer.Q("content");
            var textField = new TextField { value = Node.CustomDisplayName ?? "" };
            
            contentContainer.Remove(customNameLabel);
            contentContainer.Insert(0, textField);
            
            textField.Focus();
            textField.SelectAll();
            
            System.Action finishEditing = () =>
            {
                Node.CustomDisplayName = textField.value.Trim();
                if (textField.parent != null) textField.parent.Remove(textField);
                RefreshCustomNameDisplay();
            };
            
            textField.RegisterCallback<BlurEvent>(_ => finishEditing());
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) finishEditing();
                else if (evt.keyCode == KeyCode.Escape)
                {
                    if (textField.parent != null) textField.parent.Remove(textField);
                    RefreshCustomNameDisplay();
                }
            });
        }
        
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.Position = newPos.position;
            EditorUtility.SetDirty(Node);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 1)
            {
                GetFirstAncestorOfType<FluxGraphView>()?.OnNodeSelectionChanged(this);
            }
        }
        
        public override void OnSelected()
        {
            base.OnSelected();
            GetFirstAncestorOfType<FluxGraphView>()?.OnNodeSelectionChanged(this);
        }
        
        public Port GetInputPort(string portName) => inputContainer.Q<Port>(portName);
        public Port GetOutputPort(string portName) => outputContainer.Q<Port>(portName);
        
        private System.Type GetPortSystemType(string valueType)
        {
            return valueType.ToLower() switch {
                "bool" => typeof(bool), "int" => typeof(int), "float" => typeof(float),
                "string" => typeof(string), "vector2" => typeof(Vector2), "vector3" => typeof(Vector3),
                "void" => typeof(object), // Use a placeholder type for execution ports
                _ => Type.GetType(valueType) ?? typeof(object) // Fallback for complex types
            };
        }

        private void SetPortColor(Port port, FluxNodePort fluxPort)
        {
            port.portColor = fluxPort.PortType switch {
                FluxPortType.Data => new Color(0.6f, 0.6f, 1f), // Light Blue
                FluxPortType.Execution => new Color(0.5f, 1f, 0.5f), // Green
                _ => Color.gray
            };
        }

        private void SetNodeColor()
        {
            var color = Node.Category switch
            {
                "Audio" => new Color(0.2f, 0.6f, 1f, 1f),      // Blue
                "Data" => new Color(1f, 0.2f, 0.2f, 1f),       // Red
                "Debug" => new Color(0.6f, 1f, 0.2f, 1f),      // Lime
                "Events" => new Color(1f, 0.6f, 0.2f, 1f),     // Orange
                "Math" => new Color(0.2f, 1f, 0.6f, 1f),       // Green
                "Logic" => new Color(1f, 0.2f, 0.6f, 1f),      // Pink
                "Flow" => new Color(0.6f, 0.2f, 1f, 1f),       // Purple
                "Framework" => new Color(1f, 1f, 0.2f, 1f),    // Yellow
                "Framework/Data" => new Color(1f, 0.8f, 0.2f, 1f), // Gold
                "Framework/Components" => new Color(0.2f, 0.8f, 1f, 1f), // Sky Blue
                "Framework/Events" => new Color(0.8f, 0.2f, 0.8f, 1f), // Silver
                "Framework/Logic" => new Color(0.2f, 0.8f, 0.2f, 1f), // Light Green
                "Framework/Properties" => new Color(0.8f, 0.5f, 0.2f, 1f), // Brown
                "Framework/UI" => new Color(0.5f, 0.2f, 0.5f, 1f), // Dark Purple
                "Time" => new Color(0.2f, 1f, 1f, 1f),        // Cyan
                "UI" => new Color(1f, 0.5f, 1f, 1f),          // Magenta
                _ => new Color(0.5f, 0.5f, 0.5f, 1f)           // Gray
            };

            titleContainer.style.backgroundColor = color;
        }
    }
}