using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomNodeView(typeof(GroupNode))]
    public class GroupNodeView : FluxNodeView
    {
        public GroupNodeView(AttributedNodeWrapper nodeWrapper, FluxGraphView graphView) : base(nodeWrapper, graphView)
        {
            this.title = "Group"; // Title can be edited
            this.capabilities |= Capabilities.Groupable; // Allows being a parent for other nodes
            
            // Style it to look like a group box
            var titleLabel = this.Q<Label>("title-label");
            if (titleLabel != null) titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            
            mainContainer.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            mainContainer.style.borderTopWidth = 1;
            mainContainer.style.borderBottomWidth = 1;
            mainContainer.style.borderLeftWidth = 1;
            mainContainer.style.borderRightWidth = 1;
            mainContainer.style.borderTopColor = new StyleColor(new Color(0,0,0,0.5f));
            mainContainer.style.borderBottomColor = new StyleColor(new Color(0,0,0,0.5f));
            mainContainer.style.borderLeftColor = new StyleColor(new Color(0,0,0,0.5f));
            mainContainer.style.borderRightColor = new StyleColor(new Color(0,0,0,0.5f));

            // Hide ports
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;
            
            // Make it resizable
            this.Add(new Resizer());
            
            // Ensure it renders behind other nodes
            this.SendToBack();
        }
    }
}