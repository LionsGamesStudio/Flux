using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomNodeView(typeof(CommentNode))] 
    public class CommentNodeView : FluxNodeView
    {
        public readonly CommentNode NodeLogic;

        public CommentNodeView(AttributedNodeWrapper nodeWrapper, FluxGraphView graphView) : base(nodeWrapper, graphView)
        {
            this.NodeLogic = nodeWrapper.NodeLogic as CommentNode;
            this.title = "Comment";
            this.viewDataKey = nodeWrapper.NodeId;

            // --- Custom styling for the comment node ---
            // Remove the title bar's default color
            titleContainer.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            // Hide the input/output port containers
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;

            // Set initial position and size
            SetPosition(new Rect(nodeWrapper.Position, new Vector2(NodeLogic.Width, NodeLogic.Height)));

            // Make the node resizable
            var resizer = new Resizer();
            this.Add(resizer);
            this.style.borderBottomWidth = 10;
            this.style.borderRightWidth = 10;

            // --- Create a TextField for the comment text ---
            var textField = new TextField("")
            {
                multiline = true,
                isReadOnly = false
            };

            textField.style.flexGrow = 1; // Make it fill the available space
            textField.style.backgroundColor = new Color(1f, 1f, 0.7f, 0.1f); // Light yellow tint

            // --- Data Binding ---
            // This links the UI TextField directly to the 'CommentText' field in our CommentNode object.
            textField.BindProperty(new SerializedObject(nodeWrapper).FindProperty("_nodeLogic.CommentText"));

            // Add the text field to the node's main container
            mainContainer.Add(textField);
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            // Update the data model when the node is moved or resized
            if (NodeLogic != null)
            {
                var wrapper = this.userData as AttributedNodeWrapper;
                if (wrapper != null)
                {
                    UnityEditor.Undo.RecordObject(wrapper, "Move/Resize Comment");
                    wrapper.Position = newPos.position;
                    NodeLogic.Width = newPos.width;
                    NodeLogic.Height = newPos.height;
                    UnityEditor.EditorUtility.SetDirty(wrapper);
                }
            }
        }
    }
}