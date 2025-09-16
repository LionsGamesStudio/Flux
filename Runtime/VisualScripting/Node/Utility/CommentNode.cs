using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Comment", Category = "Utility", Description = "A resizable comment box to document your graph.")]
    public class CommentNode : INode // It doesn't need to be executable
    {
        [Tooltip("The content of the comment.")]
        [TextArea(5, 10)]
        public string CommentText;
        
        // --- Internal data for the custom editor ---
        public float Width = 300f;
        public float Height = 100f;
    }
}