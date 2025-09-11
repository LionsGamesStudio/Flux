using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using FluxFramework.VisualScripting.Nodes;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Editor.NodeViews
{
    /// <summary>
    /// A custom view for the ConstantNode that displays a dynamic value field
    /// based on the selected type, using the robust SerializedObject binding system.
    /// </summary>
    public class ConstantNodeView : FluxNodeView
    {
        // We no longer need direct references to the UI fields.
        // The PropertyField will handle everything.
        
        public new ConstantNode Node => base.Node as ConstantNode;

        public ConstantNodeView(FluxVisualGraph graph, ConstantNode node) : base(graph, node)
        {
            // The base constructor is sufficient.
        }
        
        /// <summary>
        /// This method is overridden to create the custom UI for this specific node type.
        /// </summary>
        protected override void CreateNodeContent()
        {
            // First, call the base method to create the default content (like the custom name label).
            base.CreateNodeContent();

            // Create a SerializedObject that represents our node data.
            var serializedNode = new SerializedObject(Node);

            // --- Type Dropdown ---
            var typeProp = serializedNode.FindProperty("_constantType");
            var typeField = new PropertyField(typeProp, "Type");
            
            // When the type changes, we must refresh the rest of the node's content.
            typeField.RegisterValueChangeCallback(evt =>
            {
                // We need to re-create the content to show the correct value field.
                RefreshCustomContent();
                // We also need to tell the node to update its output port.
                Node.RefreshPorts();
            });

            extensionContainer.Add(typeField);

            // --- Dynamic Value Field ---
            var currentType = (ConstantType)typeProp.enumValueIndex;
            SerializedProperty valueProp = GetValuePropertyForType(serializedNode, currentType);

            if (valueProp != null)
            {
                var valueField = new PropertyField(valueProp, "Value");
                
                // Bind the PropertyField to the SerializedObject. This automatically handles
                // updating the data when the UI is changed, and vice-versa.
                valueField.Bind(serializedNode);
                
                extensionContainer.Add(valueField);
            }
        }
        
        /// <summary>
        /// Refreshes the custom content of the node.
        /// This is called when the node's structure needs to change (e.g., when the constant type changes).
        /// </summary>
        private void RefreshCustomContent()
        {
            // Clear only the custom content we added.
            extensionContainer.Clear();
            CreateNodeContent();
        }

        /// <summary>
        /// A helper method to get the correct SerializedProperty for the value
        /// based on the currently selected ConstantType.
        /// </summary>
        private SerializedProperty GetValuePropertyForType(SerializedObject so, ConstantType type)
        {
            return type switch
            {
                ConstantType.Float => so.FindProperty("_floatValue"),
                ConstantType.Int => so.FindProperty("_intValue"),
                ConstantType.Bool => so.FindProperty("_boolValue"),
                ConstantType.String => so.FindProperty("_stringValue"),
                ConstantType.Vector2 => so.FindProperty("_vector2Value"),
                ConstantType.Vector3 => so.FindProperty("_vector3Value"),
                _ => null
            };
        }
    }
}