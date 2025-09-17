using System;
using FluxFramework.VisualScripting;

namespace FluxFramework.Attributes.VisualScripting
{
    /// <summary>
    /// Marks a field within an INode class as a port for the visual scripting graph.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PortAttribute : Attribute
    {
        /// <summary>
        /// The direction of the port (Input or Output).
        /// </summary>
        public FluxPortDirection Direction { get; }

        /// <summary>
        /// A custom name to display on the port in the editor. If null or empty, the field's name is used.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The type of the port (Data, Execution, etc.). Defaults to Data.
        /// </summary>
        public FluxPortType PortType { get; set; } = FluxPortType.Data;

        /// <summary>
        /// Defines if the port can accept single or multiple connections.
        /// Default is Multi for Outputs, Single for Inputs.
        /// </summary>
        public PortCapacity Capacity { get; set; } = PortCapacity.Single;

        /// <summary>
        /// A descriptive tooltip that will appear when hovering over the port in the editor.
        /// </summary>
        public string Tooltip { get; set; }

        public PortAttribute(FluxPortDirection direction)
        {
            Direction = direction;
        }

        public PortAttribute(FluxPortDirection direction, FluxPortType portType, PortCapacity capacity)
        {
            Direction = direction;
            PortType = portType;
            Capacity = capacity;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, PortCapacity capacity)
        {
            Direction = direction;
            DisplayName = displayName;
            Capacity = capacity;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, string tooltip, PortCapacity capacity)
        {
            Direction = direction;
            DisplayName = displayName;
            Tooltip = tooltip;
            Capacity = capacity;
        }

        public PortAttribute(FluxPortDirection direction, FluxPortType portType, string tooltip, PortCapacity capacity)
        {
            Direction = direction;
            PortType = portType;
            Tooltip = tooltip;
            Capacity = capacity;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, FluxPortType portType, PortCapacity capacity)
        {
            Direction = direction;
            DisplayName = displayName;
            PortType = portType;
            Capacity = capacity;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, FluxPortType portType, string tooltip, PortCapacity capacity)
        {
            Direction = direction;
            DisplayName = displayName;
            PortType = portType;
            Tooltip = tooltip;
            Capacity = capacity;
        }
    }
}