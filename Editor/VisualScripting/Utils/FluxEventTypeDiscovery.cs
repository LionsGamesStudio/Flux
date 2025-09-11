using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using FluxFramework.Events;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// Utility class to discover available event types in the project
    /// </summary>
    public static class FluxEventTypeDiscovery
    {
        private static List<string> _cachedEventTypes;
        private static bool _isDiscovered = false;

        // Manually excluded event types (can be extended)
        private static readonly HashSet<string> ExcludedTypes = new HashSet<string>
        {
            "IFluxEvent", // Interface itself
            "FluxEventBase", // Base classes
            "FluxEventArgs", // Argument classes
            "EventArgs", // System event args
            // Add any other types you want to exclude
        };

        /// <summary>
        /// Get all available event types in the project
        /// </summary>
        public static List<string> GetAvailableEventTypes()
        {
            if (!_isDiscovered)
            {
                DiscoverEventTypes();
            }
            return new List<string>(_cachedEventTypes);
        }

        /// <summary>
        /// Refresh the cache of event types
        /// </summary>
        public static void RefreshEventTypes()
        {
            _isDiscovered = false;
            DiscoverEventTypes();
        }

        private static void DiscoverEventTypes()
        {
            _cachedEventTypes = new List<string>();

            // Discover IFluxEvent implementations
            var eventTypes = DiscoverFluxEventTypes();
            _cachedEventTypes.AddRange(eventTypes);

            // Discover classes with FluxEvent attribute
            var attributeEvents = DiscoverAttributedEvents();
            _cachedEventTypes.AddRange(attributeEvents);

            // Discover events from FluxEventDefinitions configurations
            var configEvents = DiscoverConfigurationEvents();
            _cachedEventTypes.AddRange(configEvents);

            // Remove duplicates and sort
            _cachedEventTypes = _cachedEventTypes.Distinct().OrderBy(x => x).ToList();

            // Filter out any remaining non-events
            _cachedEventTypes = _cachedEventTypes.Where(IsValidEventName).ToList();

            _isDiscovered = true;
            Debug.Log($"[FluxFramework] Discovered {_cachedEventTypes.Count} event types");
        }

        /// <summary>
        /// Check if an event name looks like a valid event
        /// </summary>
        private static bool IsValidEventName(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return false;

            // Check manual exclusion list
            if (ExcludedTypes.Contains(eventName))
                return false;

            // Must contain "Event" somewhere
            if (!eventName.Contains("Event"))
                return false;

            // Exclude obvious non-events
            if (eventName.Contains("Manager") ||
                eventName.Contains("Handler") ||
                eventName.Contains("System") ||
                eventName.Contains("Service") ||
                eventName.Contains("Provider") ||
                eventName.Contains("Factory") ||
                eventName.Contains("Builder") ||
                eventName.Contains("Helper") ||
                eventName.Contains("Utility") ||
                eventName.Contains("Base") ||
                eventName.Contains("Abstract"))
                return false;

            return true;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor menu item to refresh event types
        /// </summary>
        [UnityEditor.MenuItem("Flux/Visual Scripting/Refresh Event Types")]
        public static void EditorRefreshEventTypes()
        {
            RefreshEventTypes();
            UnityEditor.EditorUtility.DisplayDialog("Flux Framework", 
                $"Refreshed event types cache. Found {_cachedEventTypes?.Count ?? 0} event types.", "OK");
        }
        #endif

        private static List<string> DiscoverFluxEventTypes()
        {
            var eventTypes = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    // Skip system assemblies
                    if (IsSystemAssembly(assembly.FullName))
                        continue;

                    var types = assembly.GetTypes()
                        .Where(t => typeof(IFluxEvent).IsAssignableFrom(t) && 
                                   !t.IsInterface && 
                                   !t.IsAbstract &&
                                   IsValidEventType(t));

                    foreach (var type in types)
                    {
                        // Use class name as event type
                        var eventName = type.Name;
                        eventTypes.Add(eventName);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FluxFramework] Could not scan assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return eventTypes;
        }

        private static bool IsValidEventType(Type type)
        {
            // Must be a concrete class that implements IFluxEvent
            if (type.IsInterface || type.IsAbstract)
                return false;

            // Must have a public parameterless constructor (typical for events)
            var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor == null || !defaultConstructor.IsPublic)
                return false;

            // Exclude base classes or utilities
            if (type.Name.Contains("Base") || 
                type.Name.Contains("Manager") || 
                type.Name.Contains("Handler") ||
                type.Name.Contains("Processor") ||
                type.Name.Contains("Helper") ||
                type.Name.Contains("Utility"))
                return false;

            // Should end with "Event" or at least contain "Event" 
            if (!type.Name.EndsWith("Event") && !type.Name.Contains("Event"))
                return false;

            return true;
        }

        private static List<string> DiscoverAttributedEvents()
        {
            var eventTypes = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (IsSystemAssembly(assembly.FullName))
                        continue;

                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        // Only look for classes with specific event-related attributes
                        var attributes = type.GetCustomAttributes(false);
                        foreach (var attr in attributes)
                        {
                            var attrName = attr.GetType().Name;
                            
                            // Only specific event attributes
                            if (attrName == "FluxEventAttribute" || 
                                attrName == "EventAttribute" ||
                                attrName == "GameEventAttribute")
                            {
                                // Additional validation that this is actually an event
                                if (IsValidEventType(type))
                                {
                                    eventTypes.Add(type.Name);
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FluxFramework] Could not scan assembly {assembly.FullName}: {ex.Message}");
                }
            }

            return eventTypes;
        }

        private static List<string> DiscoverConfigurationEvents()
        {
            var eventTypes = new List<string>();

            try
            {
                // Find all FluxEventDefinitions assets in the project
                var guids = UnityEditor.AssetDatabase.FindAssets("t:FluxEventDefinitions");
                
                foreach (var guid in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var eventDefinitions = UnityEditor.AssetDatabase.LoadAssetAtPath<FluxFramework.Configuration.FluxEventDefinitions>(path);
                    
                    if (eventDefinitions != null && eventDefinitions.events != null)
                    {
                        foreach (var eventDef in eventDefinitions.events)
                        {
                            if (!string.IsNullOrEmpty(eventDef.eventName))
                            {
                                eventTypes.Add(eventDef.eventName);
                            }
                        }
                    }
                }
                
                Debug.Log($"[FluxFramework] Found {eventTypes.Count} events from configuration assets");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FluxFramework] Could not scan configuration events: {ex.Message}");
            }

            return eventTypes;
        }

        private static bool IsSystemAssembly(string assemblyName)
        {
            return assemblyName.StartsWith("System.") ||
                   assemblyName.StartsWith("Microsoft.") ||
                   assemblyName.StartsWith("Unity.") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   assemblyName.StartsWith("UnityEditor") ||
                   assemblyName.StartsWith("mscorlib") ||
                   assemblyName.StartsWith("netstandard");
        }
    }
}
