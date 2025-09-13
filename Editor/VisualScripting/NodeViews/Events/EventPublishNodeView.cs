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
    /// A custom view for the EventPublishNode that provides an event type dropdown,
    /// using the robust SerializedObject binding system.
    /// </summary>
    public class EventPublishNodeView : FluxNodeView
    {
        public new EventPublishNode Node => base.Node as EventPublishNode;

        public EventPublishNodeView(FluxVisualGraph graph, EventPublishNode node) : base(graph, node)
        {
            // The base constructor handles initial setup.
            // Custom content is created via the overridden method.
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
            
            // Ensure the current value is in the list, even if it was manually typed.
            if (!string.IsNullOrEmpty(currentEventType) && !availableEventTypes.Contains(currentEventType))
            {
                availableEventTypes.Insert(1, currentEventType); // Insert after the "Generic" option
            }

            var typeDropdown = new DropdownField("Event Type", availableEventTypes, currentEventType);
            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                eventTypeProp.stringValue = evt.newValue;
                serializedNode.ApplyModifiedProperties();
            });
            
            extensionContainer.Add(typeDropdown);

            // --- Manual Entry ---
            var customEventField = new TextField("Manual Entry");
            customEventField.BindProperty(eventTypeProp); // Direct two-way binding!
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
                    .Select(t => t.FullName)
                    .OrderBy(name => name)
                    .ToList();
                
                // Add an option for publishing a GenericFluxEvent
                eventTypes.Insert(0, ""); // An empty string represents the GenericFluxEvent
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