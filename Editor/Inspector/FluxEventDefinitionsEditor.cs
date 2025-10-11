using UnityEngine;
using UnityEditor;
using FluxFramework.Configuration;
using FluxFramework.Core;
using System.Collections.Generic;
using System.Linq;
using System;

namespace FluxFramework.Editor
{
    [CustomEditor(typeof(FluxEventDefinitions))]
    public class FluxEventDefinitionsEditor : UnityEditor.Editor
    {
        private FluxEventDefinitions _targetAsset;

        private void OnEnable()
        {
            _targetAsset = (FluxEventDefinitions)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Automatic Discovery", EditorStyles.boldLabel);

            if (GUILayout.Button("Scan Project for Event Types"))
            {
                ScanAndAddEvents();
            }
            EditorGUILayout.HelpBox("Scans the project for all classes that implement IFluxEvent and adds any new ones to this list.", MessageType.Info);
        }

        private void ScanAndAddEvents()
        {
            int eventsAdded = 0;
            var existingEventNames = new HashSet<string>(_targetAsset.events.Select(e => e.eventName));

            // Use TypeCache to efficiently find all non-abstract classes that implement IFluxEvent
            var eventTypes = TypeCache.GetTypesDerivedFrom<IFluxEvent>()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var type in eventTypes)
            {
                string typeName = type.Name; // We'll use the short name for user-friendliness

                if (!existingEventNames.Contains(typeName))
                {
                    var newDefinition = new EventDefinition
                    {
                        eventName = typeName,
                        description = $"Discovered from class: {type.FullName}",
                        isGlobal = true,
                        debugColor = GetColorForEventType(typeName)
                    };
                    
                    _targetAsset.events.Add(newDefinition);
                    existingEventNames.Add(typeName);
                    eventsAdded++;
                }
            }

            if (eventsAdded > 0)
            {
                // Sort the list alphabetically for consistency
                _targetAsset.events = _targetAsset.events.OrderBy(e => e.eventName).ToList();
                EditorUtility.SetDirty(_targetAsset);
                AssetDatabase.SaveAssets();
                FluxFramework.Core.Flux.Manager.Logger.Info($"[FluxFramework] Discovery complete. Added {eventsAdded} new event definition(s) to '{_targetAsset.name}'.");
            }
            else
            {
                FluxFramework.Core.Flux.Manager.Logger.Info("[FluxFramework] Discovery complete. No new event definitions found.", _targetAsset);
            }
        }
        
        private Color GetColorForEventType(string typeName)
        {
            int hash = typeName.GetHashCode();
            float h = (hash & 0xFF) / 255.0f;
            return Color.HSVToRGB(h, 0.6f, 0.9f);
        }
    }
}