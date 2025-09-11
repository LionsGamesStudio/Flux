using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using FluxFramework.VisualScripting.Nodes;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Graphs;

namespace FluxFramework.VisualScripting.Editor.NodeViews
{
    /// <summary>
    /// A custom view for the EventListenerNode that provides an event type dropdown,
    /// using the robust SerializedObject binding system.
    /// </summary>
    public class EventListenerNodeView : FluxNodeView
    {
        public new EventListenerNode Node => base.Node as EventListenerNode;

        public EventListenerNodeView(FluxVisualGraph graph, EventListenerNode node) : base(graph, node)
        {
            // The base class constructor and the overridden CreateNodeContent
            // are now sufficient for all setup.
        }

        /// <summary>
        /// This method is overridden to create the custom UI for this specific node type.
        /// </summary>
        protected override void CreateNodeContent()
        {
            base.CreateNodeContent();

            var serializedNode = new SerializedObject(Node);
            var eventTypeProp = serializedNode.FindProperty("_eventType");
            
            // --- Event Type Dropdown ---
            
            var availableEventTypes = GetAvailableEventTypes();
            var currentEventType = eventTypeProp.stringValue;
            
            if (!string.IsNullOrEmpty(currentEventType) && !availableEventTypes.Contains(currentEventType))
            {
                availableEventTypes.Insert(1, currentEventType); // Insert after the "None" option
            }

            var typeDropdown = new DropdownField("Event Type", availableEventTypes, currentEventType);
            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                eventTypeProp.stringValue = evt.newValue;
                serializedNode.ApplyModifiedProperties();
            });
            
            extensionContainer.Add(typeDropdown);

            // --- Manual Entry Field (for convenience) ---
            // This field is two-way bound to the same property as the dropdown.
            // Changing one will update the other.
            var customEventField = new TextField("Manual Entry");
            customEventField.BindProperty(eventTypeProp);
            extensionContainer.Add(customEventField);
        }

        /// <summary>
        /// Discovers all available IFluxEvent types using Unity's TypeCache.
        /// </summary>
        private List<string> GetAvailableEventTypes()
        {
            try
            {
                var eventTypes = TypeCache.GetTypesDerivedFrom<IFluxEvent>()
                    .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .Select(t => t.FullName) // Use FullName to avoid ambiguity
                    .OrderBy(name => name)
                    .ToList();
                
                eventTypes.Insert(0, ""); // Add a "None" option
                return eventTypes;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to discover event types: {ex.Message}");
                return new List<string> { "" };
            }
        }
    }
}