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
        /// A descriptive tooltip that will appear when hovering over the port in the editor.
        /// </summary>
        public string Tooltip { get; set; }

        public PortAttribute(FluxPortDirection direction)
        {
            Direction = direction;
        }

        public PortAttribute(FluxPortDirection direction, FluxPortType portType)
        {
            Direction = direction;
            PortType = portType;
        }

        public PortAttribute(FluxPortDirection direction, string displayName)
        {
            Direction = direction;
            DisplayName = displayName;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, string tooltip)
        {
            Direction = direction;
            DisplayName = displayName;
            Tooltip = tooltip;
        }

        public PortAttribute(FluxPortDirection direction, FluxPortType portType, string tooltip)
        {
            Direction = direction;
            PortType = portType;
            Tooltip = tooltip;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, FluxPortType portType = FluxPortType.Data)
        {
            Direction = direction;
            DisplayName = displayName;
            PortType = portType;
        }

        public PortAttribute(FluxPortDirection direction, string displayName, FluxPortType portType, string tooltip)
        {
            Direction = direction;
            DisplayName = displayName;
            PortType = portType;
            Tooltip = tooltip;
        }
    }
}