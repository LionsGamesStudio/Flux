using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using FluxFramework.VisualScripting.Nodes;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Editor.NodeViews
{
    /// <summary>
    /// Custom node view for StartNode with distinctive styling
    /// </summary>
    public class StartNodeView : FluxNodeView
    {
        public StartNodeView(FluxVisualGraph graph, StartNode node) : base(graph, node)
        {
        }

        protected void SetupNodeStyle()
        {
            // Give the StartNode a distinctive green color
            style.backgroundColor = new Color(0.2f, 0.7f, 0.3f, 0.8f); // Green
            
            // Add a distinctive border
            style.borderTopColor = Color.green;
            style.borderBottomColor = Color.green;
            style.borderLeftColor = Color.green;
            style.borderRightColor = Color.green;
            style.borderTopWidth = 2;
            style.borderBottomWidth = 2;
            style.borderLeftWidth = 2;
            style.borderRightWidth = 2;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
        }

        protected override void CreateNodeContent()
        {
            base.CreateNodeContent();
            
            // Add a start icon or indicator
            var startLabel = new Label("🚀");
            startLabel.style.fontSize = 16;
            startLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            startLabel.style.color = Color.white;
            startLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Insert at the beginning
            titleContainer.Insert(0, startLabel);
        }

        protected string GetNodeDisplayName()
        {
            return "🚀 Start";
        }
    }
}
