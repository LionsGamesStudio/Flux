using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Group", Category = "Utility", Description = "A resizable group box to visually organize nodes.")]
    public class GroupNode : INode
    {
        // The title will be edited directly in the Graph View
        
        // --- Internal data for the custom view ---
        public float Width = 400f;
        public float Height = 300f;
    }
}