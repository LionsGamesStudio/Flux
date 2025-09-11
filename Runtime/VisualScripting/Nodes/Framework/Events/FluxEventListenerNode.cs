using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that listens for a specific FluxEvent from the framework's central EventBus
    /// and triggers an execution flow when the event is received.
    /// </summary>
    [CreateAssetMenu(fileName = "FluxEventListenerNode", menuName = "Flux/Visual Scripting/Framework/Events/Flux Event Listener")]
    public class FluxEventListenerNode : FluxNodeBase
    {
        [Tooltip("The fully qualified name of the event type to listen for (e.g., 'MyGame.Events.PlayerDiedEvent').")]
        [SerializeField] private string _eventType = "";
        
        private readonly Dictionary<GameObject, IDisposable> _subscriptions = new Dictionary<GameObject, IDisposable>();

        public override string NodeName => "Flux Event Listener";
        public override string Category => "Framework/Events";

        public string EventType 
        { 
            get => _eventType; 
            set { _eventType = value; NotifyChanged(); } 
        }

        protected override void InitializePorts()
        {
            AddInputPort("subscribe", "▶ Subscribe", FluxPortType.Execution, "void", false);
            AddInputPort("unsubscribe", "▶ Unsubscribe", FluxPortType.Execution, "void", false);
            AddInputPort("context", "Context", FluxPortType.Data, "GameObject", true, null, "The GameObject running the graph, required for safe subscription.");
            AddInputPort("eventType", "Event Type Name", FluxPortType.Data, "string", false, _eventType);
            
            AddOutputPort("onEvent", "▶ On Event", FluxPortType.Execution, "void", false);
            AddOutputPort("onSubscribed", "▶ On Subscribed", FluxPortType.Execution, "void", false);
            AddOutputPort("onUnsubscribed", "▶ On Unsubscribed", FluxPortType.Execution, "void", false);
            
            AddOutputPort("eventData", "Event Data", FluxPortType.Data, "IFluxEvent", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var runner = executor.Runner;

            if (inputs.ContainsKey("subscribe"))
            {
                string eventTypeName = GetInputValue<string>(inputs, "eventType", _eventType);
                SubscribeToEvent(executor, eventTypeName, outputs);
            }
            if (inputs.ContainsKey("unsubscribe"))
            {
                UnsubscribeFromEvent(runner, outputs);
            }
        }

        private void SubscribeToEvent(FluxGraphExecutor executor, string eventTypeName, Dictionary<string, object> outputs)
        {
            var context = executor.Runner.GetContextObject();
            if (context == null)
            {
                Debug.LogError("FluxEventListenerNode: A 'Context' GameObject is required to subscribe.", this);
                return;
            }
            if (string.IsNullOrEmpty(eventTypeName)) return;

            UnsubscribeFromEvent(executor.Runner, null);

            try
            {
                Type eventType = Type.GetType(eventTypeName);
                if (eventType == null || !typeof(IFluxEvent).IsAssignableFrom(eventType))
                {
                    Debug.LogError($"FluxEventListenerNode: Event type '{eventTypeName}' not found or does not implement IFluxEvent.", this);
                    return;
                }
                
                var subscribeMethod = typeof(EventBus).GetMethod("Subscribe", new[] { typeof(Action<>).MakeGenericType(eventType), typeof(int) });
                
                Action<IFluxEvent> handler = (evt) => OnEventReceived(executor, evt);
                var typedDelegate = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(eventType), handler.Target, handler.Method);

                var subscription = (IDisposable)subscribeMethod.Invoke(null, new object[] { typedDelegate, 0 });
                _subscriptions[context] = subscription;

                var cleanup = context.GetComponent<GraphSubscriptionCleanup>() ?? context.AddComponent<GraphSubscriptionCleanup>();
                cleanup.AddSubscription(subscription);

                if (outputs != null) SetOutputValue(outputs, "onSubscribed", null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FluxEventListenerNode: Failed to subscribe to '{eventTypeName}': {ex.Message}", this);
            }
        }

        private void UnsubscribeFromEvent(IGraphRunner runner, Dictionary<string, object> outputs)
        {
             var context = runner.GetContextObject();
             if (context == null) return;

            if (_subscriptions.TryGetValue(context, out var subscription))
            {
                subscription.Dispose();
                _subscriptions.Remove(context);
                if (outputs != null) SetOutputValue(outputs, "onUnsubscribed", null);
            }
        }

        private void OnEventReceived(FluxGraphExecutor executor, IFluxEvent evt)
        {
            var outputs = new Dictionary<string, object>
            {
                { "eventData", evt }
            };
            executor.ContinueFromPort(this, "onEvent", outputs);
        }

        protected void OnDestroy()
        {
            foreach (var sub in _subscriptions.Values) sub.Dispose();
            _subscriptions.Clear();
        }
    }
}