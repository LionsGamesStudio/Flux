using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that publishes an event through the FluxFramework EventBus.
    /// It can publish a generic event with custom data, or a specific, strongly-typed event.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxEventPublishNode", menuName = "Flux/Visual Scripting/Framework/Events/Flux Event Publish")]
    public class FluxEventPublishNode : FluxNodeBase
    {
        [Tooltip("The fully qualified name of the event type to publish. If empty, a GenericFluxEvent will be published.")]
        [SerializeField] private string _eventType = "";

        [Tooltip("The source identifier for this event, useful for debugging.")]
        [SerializeField] private string _source = "VisualScripting";

        public override string NodeName => "Publish Flux Event";
        public override string Category => "Framework/Events";

        public string EventType { get => _eventType; set { _eventType = value; NotifyChanged(); } }
        public string Source { get => _source; set { _source = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("eventType", "Event Type Name", FluxPortType.Data, "string", false, _eventType);
            AddInputPort("source", "Source", FluxPortType.Data, "string", false, _source);
            AddInputPort("data", "Data (Generic)", FluxPortType.Data, "object", false, null);
            AddInputPort("metadata", "Metadata (Generic)", FluxPortType.Data, "Dictionary<string,object>", false, null);
            
            AddOutputPort("onPublished", "▶ Out", FluxPortType.Execution, "void", false);
            AddOutputPort("success", "Success", FluxPortType.Data, "bool", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            string eventTypeName = GetInputValue<string>(inputs, "eventType", _eventType);
            string source = GetInputValue<string>(inputs, "source", _source);
            object data = GetInputValue<object>(inputs, "data", null);
            var metadata = GetInputValue<Dictionary<string, object>>(inputs, "metadata", null);

            try
            {
                IFluxEvent eventToPublish;

                if (string.IsNullOrEmpty(eventTypeName))
                {
                    eventToPublish = new GenericFluxEvent(source, data, metadata);
                }
                else
                {
                    Type eventType = Type.GetType(eventTypeName);
                    if (eventType == null || !typeof(IFluxEvent).IsAssignableFrom(eventType))
                    {
                        Debug.LogError($"FluxEventPublishNode: Event type '{eventTypeName}' not found or does not implement IFluxEvent.", this);
                        SetOutputValue(outputs, "success", false);
                        return;
                    }
                    // This assumes the event has a parameterless constructor.
                    eventToPublish = (IFluxEvent)Activator.CreateInstance(eventType);
                }
                
                var publishMethod = typeof(EventBus).GetMethod("Publish").MakeGenericMethod(eventToPublish.GetType());
                publishMethod.Invoke(null, new object[] { eventToPublish });

                SetOutputValue(outputs, "success", true);
                SetOutputValue(outputs, "onPublished", null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FluxEventPublishNode: Failed to publish event '{eventTypeName}': {ex.Message}\n{ex.StackTrace}", this);
                SetOutputValue(outputs, "success", false);
            }
        }
    }

    /// <summary>
    /// A generic event container used by the visual scripting system
    /// to publish events without needing a specific C# class.
    /// </summary>
    public class GenericFluxEvent : FluxEventBase
    {
        public object Data { get; }
        public Dictionary<string, object> Metadata { get; }

        public GenericFluxEvent(string source, object data = null, Dictionary<string, object> metadata = null) 
            : base(source)
        {
            Data = data;
            Metadata = metadata ?? new Dictionary<string, object>();
        }
    }
}