using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that publishes (invokes) a standard static C# event using reflection.
    /// This is used to communicate with systems outside of the Flux EventBus.
    /// </summary>
    [CreateAssetMenu(fileName = "EventPublishNode", menuName = "Flux/Visual Scripting/Events/C# Event Publish")]
    public class EventPublishNode : FluxNodeBase
    {
        [Tooltip("The fully qualified name of the class containing the static event (e.g., 'MyGame.MyEventManager').")]
        [SerializeField] private string _targetClassType = "";
        [Tooltip("The name of the static C# event to publish (e.g., 'OnPlayerScored').")]
        [SerializeField] private string _eventName = "";

        public override string NodeName => "Publish C# Event";
        public override string Category => "Events";

        public string TargetClassType { get => _targetClassType; set { _targetClassType = value; NotifyChanged(); } }
        public string EventName { get => _eventName; set { _eventName = value; NotifyChanged(); } }

        protected override void InitializePorts()
        {
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddInputPort("eventArgs", "Arguments", FluxPortType.Data, "object[]", false, null, "An array of arguments to pass to the event handlers.");
            
            AddOutputPort("onPublished", "▶ Out", FluxPortType.Execution, "void", false);
            AddOutputPort("onFailure", "▶ On Failure", FluxPortType.Execution, "void", false);
            AddOutputPort("success", "Success", FluxPortType.Data, "bool", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute")) return;

            if (string.IsNullOrEmpty(_targetClassType) || string.IsNullOrEmpty(_eventName))
            {
                Debug.LogError("EventPublishNode: Target Class Type and Event Name must be specified.", this);
                SetOutputValue(outputs, "success", false);
                SetOutputValue(outputs, "onFailure", null);
                return;
            }

            try
            {
                Type targetType = Type.GetType(_targetClassType);
                if (targetType == null)
                {
                    Debug.LogError($"EventPublishNode: Could not find type '{_targetClassType}'.", this);
                    SetOutputValue(outputs, "success", false);
                    SetOutputValue(outputs, "onFailure", null);
                    return;
                }
                
                FieldInfo eventField = targetType.GetField(_eventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                
                if (eventField != null && eventField.GetValue(null) is MulticastDelegate eventDelegate)
                {
                    object[] eventArgs = GetInputValue<object[]>(inputs, "eventArgs");
                    eventDelegate.DynamicInvoke(eventArgs);
                    
                    SetOutputValue(outputs, "success", true);
                    SetOutputValue(outputs, "onPublished", null);
                }
                else
                {
                    Debug.LogWarning($"EventPublishNode: Event '{_eventName}' on type '{_targetClassType}' has no subscribers or could not be found.", this);
                    SetOutputValue(outputs, "success", true);
                    SetOutputValue(outputs, "onPublished", null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventPublishNode: Error publishing event '{_eventName}': {ex.Message}\n{ex.StackTrace}", this);
                SetOutputValue(outputs, "success", false);
                SetOutputValue(outputs, "onFailure", null);
            }
        }
    }
}