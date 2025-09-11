using System;
using UnityEngine;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Conditional attribute that shows/hides fields based on other field values
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxConditionalAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the field to check
        /// </summary>
        public string ConditionField { get; }

        /// <summary>
        /// Expected value for the condition
        /// </summary>
        public object ExpectedValue { get; }

        /// <summary>
        /// Whether to show when condition is true or false
        /// </summary>
        public bool ShowWhenTrue { get; set; } = true;

        public FluxConditionalAttribute(string conditionField, object expectedValue)
        {
            ConditionField = conditionField;
            ExpectedValue = expectedValue;
        }
    }

    /// <summary>
    /// Creates a group header in the inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxGroupAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the group
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Whether the group is collapsible
        /// </summary>
        public bool Collapsible { get; set; } = true;

        /// <summary>
        /// Whether the group starts collapsed
        /// </summary>
        public bool StartCollapsed { get; set; } = false;

        /// <summary>
        /// Order of the group
        /// </summary>
        public int Order { get; set; } = 0;

        public FluxGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

    /// <summary>
    /// Adds a button in the inspector that calls a method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FluxButtonAttribute : Attribute
    {
        /// <summary>
        /// Text to display on the button
        /// </summary>
        public string ButtonText { get; }

        /// <summary>
        /// Whether the button is enabled during play mode
        /// </summary>
        public bool EnabledInPlayMode { get; set; } = true;

        /// <summary>
        /// Whether the button is enabled in edit mode
        /// </summary>
        public bool EnabledInEditMode { get; set; } = true;

        /// <summary>
        /// Space above the button
        /// </summary>
        public int SpaceAbove { get; set; } = 5;

        public FluxButtonAttribute(string buttonText = null)
        {
            ButtonText = buttonText;
        }
    }

    /// <summary>
    /// Marks a field as a performance critical property that should be monitored
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class FluxPerformanceAttribute : Attribute
    {
        /// <summary>
        /// Category for performance monitoring
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Whether to log performance metrics
        /// </summary>
        public bool LogMetrics { get; set; } = true;

        /// <summary>
        /// Threshold in milliseconds for performance warnings
        /// </summary>
        public float WarningThreshold { get; set; } = 16.67f; // 60 FPS

        public FluxPerformanceAttribute(string category = "General")
        {
            Category = category;
        }
    }
}
