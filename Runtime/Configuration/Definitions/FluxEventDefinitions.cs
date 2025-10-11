using UnityEngine;
using System.Collections.Generic;
using FluxFramework.Attributes;
using FluxFramework.Core;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Event definitions for the framework
    /// </summary>
    [FluxConfiguration("Events", 
        DisplayName = "Event Definitions", 
        Description = "Event type definitions and debugging configuration",
        LoadPriority = 85,
        DefaultMenuPath = "Flux/Event Definitions")]
    [CreateAssetMenu(fileName = "FluxEventDefinitions", menuName = "Flux/Event Definitions")]
    public class FluxEventDefinitions : FluxConfigurationAsset
    {
        [Header("Event Definitions")]
        public List<EventDefinition> events = new List<EventDefinition>();

        [Header("Debug Settings")]
        public bool logEventDispatch = false;
        public bool showEventTimeline = false;

        public override bool ValidateConfiguration()
        {
            var names = new HashSet<string>();
            
            foreach (var eventDef in events)
            {
                if (string.IsNullOrEmpty(eventDef.eventName))
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error("[FluxFramework] Event name cannot be empty");
                    return false;
                }

                if (names.Contains(eventDef.eventName))
                {
                    FluxFramework.Core.Flux.Manager.Logger.Error($"[FluxFramework] Duplicate event name: {eventDef.eventName}");
                    return false;
                }

                names.Add(eventDef.eventName);
            }

            return true;
        }

        public override void ApplyConfiguration(IFluxManager manager)
        {
            if (!ValidateConfiguration()) return;

            // Register events with the framework
            foreach (var eventDef in events)
            {
                FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Registered event: {eventDef.eventName}");
            }
        }
    }
}
