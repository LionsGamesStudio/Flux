using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Displays a group of buttons in the inspector for methods marked with FluxButton
    /// Place this attribute on any field to show all FluxButton methods of the class
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FluxButtonGroupAttribute : PropertyAttribute
    {
        /// <summary>
        /// Title to display above the button group
        /// </summary>
        public string Title { get; set; } = "Debug Methods";
        
        /// <summary>
        /// Whether to show buttons during play mode
        /// </summary>
        public bool ShowInPlayMode { get; set; } = true;
        
        /// <summary>
        /// Whether to show buttons in edit mode
        /// </summary>
        public bool ShowInEditMode { get; set; } = true;

        public FluxButtonGroupAttribute(string title = "Debug Methods")
        {
            Title = title;
        }
    }
}
