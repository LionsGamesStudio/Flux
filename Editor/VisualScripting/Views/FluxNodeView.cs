using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.Editor;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxNodeView : UnityEditor.Experimental.GraphView.Node 
    {
        private Dictionary<string, Label> _debugLabels = new Dictionary<string, Label>();

        private Color _originalHeaderColor;
        private Color _highlightHeaderColor;
        private IVisualElementScheduledItem _highlightResetter;

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
                var portView = Port.Create<AnimatedFluxEdge>(Orientation.Horizontal, Direction.Input, capacity, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                portView.userData = portData; // Store the port data for later use

                portView.AddManipulator(new EdgeConnector<AnimatedFluxEdge>(edgeListener));

                inputContainer.Add(portView);
            }
        }
        
        private void CreateOutputPorts(IEdgeConnectorListener listener)
        {
            foreach (var portData in Node.OutputPorts)
            {
                var capacity = (portData.Capacity == PortCapacity.Multi) ? Port.Capacity.Multi : Port.Capacity.Single;
                var portView = Port.Create<AnimatedFluxEdge>(Orientation.Horizontal, Direction.Output, capacity, GetPortType(portData));
                portView.portName = portData.DisplayName;
                portView.name = portData.Name; // Used for querying
                portView.userData = portData; // Store the port data for later use

                portView.AddManipulator(new EdgeConnector<AnimatedFluxEdge>(listener));

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
        /// Called when the graph executor enters this node.
        /// Applies a highlight effect to the node's header.
        /// </summary>
        public void TriggerEnterAnimation()
        {
            // Cancel any previous resetter that might be scheduled.
            _highlightResetter?.Pause();

            // We use a scheduler to ensure the change happens on the UI thread,
            // which can be important if the event comes from another thread.
            schedule.Execute(() => titleContainer.style.backgroundColor = _highlightHeaderColor);
        }

        /// <summary>
        /// Called when the graph executor exits this node.
        /// Removes the highlight effect from the node's header.
        /// </summary>
        public void TriggerExitAnimation()
        {
            _highlightResetter = schedule.Execute(() => 
            {
                titleContainer.style.backgroundColor = _originalHeaderColor;
            }).StartingIn(200);
        }

        /// <summary>
        /// Resets all visual debugging effects on this node to their default state.
        /// </summary>
        public void ResetVisuals()
        {
            // Restore the original header color
            _highlightResetter?.Pause();
        
            if (titleContainer != null)
            {
                titleContainer.style.backgroundColor = _originalHeaderColor;
            }
            ClearPortDebugValues();
        }


        /// <summary>
        /// Removes all debug labels from this node. Called when exiting play mode.
        /// </summary>
        private void ClearPortDebugValues()
        {
            foreach (var label in _debugLabels.Values)
            {
                label.RemoveFromHierarchy();
            }
            _debugLabels.Clear();
        }

        private void ApplyCategoryColor()
        {
            FluxGraphTheme theme = null;

            // 1. Search for a theme asset anywhere in the project (Assets folder, other packages).
            var themeGuids = AssetDatabase.FindAssets("t:FluxGraphTheme");
            if (themeGuids.Length > 0)
            {
                // If the user has created one or more themes, we use the first one we find.
                var themePath = AssetDatabase.GUIDToAssetPath(themeGuids[0]);
                theme = AssetDatabase.LoadAssetAtPath<FluxGraphTheme>(themePath); 
                
                if (themeGuids.Length > 1)
                {
                    Debug.LogWarning("Multiple FluxGraphTheme assets found. Using the one at: " + themePath);
                }
            }
            
            // 2. If no theme was found in the entire project, load our default theme from the package.
            if (theme == null)
            {
                // a. IMPORTANT: Define the relative path to your theme FROM THE ROOT of your package.
                //    Example: "Editor/Assets/Themes/FluxGraphTheme.asset"
                string relativeThemePath = "Editor/Resources/FluxGraphTheme.asset";

                // b. Get the full, valid path using your utility class.
                string defaultThemePath = FluxEditorPaths.GetFullPath(relativeThemePath);

                if (string.IsNullOrEmpty(defaultThemePath))
                {
                    Debug.LogError("[FluxNodeView] Could not get the package root path. Cannot load default theme.");
                    return;
                }
                
                theme = AssetDatabase.LoadAssetAtPath<FluxGraphTheme>(defaultThemePath);
                
                if (theme == null)
                {
                    Debug.LogError("[FluxNodeView] Could not find the default FluxGraphTheme asset at expected path: " + defaultThemePath);
                    return;
                }
            }

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
            
            // 1. Save the original color.
            _originalHeaderColor = headerColor;

            // 2. Calculate the highlight color by blending with white.
            // A 'blendFactor' of 0.4 means 40% white and 60% original color.
            // This creates a very noticeable but pleasant "glow" effect.
            float blendFactor = 0.4f; 
            _highlightHeaderColor = Color.Lerp(_originalHeaderColor, Color.white, blendFactor);
            
            // Ensure the final alpha is not washed out. We can keep the original's alpha.
            _highlightHeaderColor.a = _originalHeaderColor.a;


            // 3. Apply the initial (original) color to the header.
            titleContainer.style.backgroundColor = _originalHeaderColor;
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