using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that listens to standard C# events (non-Flux events) from any public static class.
    /// It uses reflection to subscribe and unsubscribe.
    /// </summary>
    [CreateAssetMenu(fileName = "EventListenerNode", menuName = "Flux/Visual Scripting/Events/C# Event Listener")]
    public class EventListenerNode : FluxNodeBase
    {
        [Tooltip("The fully qualified name of the class containing the event (e.g., 'MyGame.MyEventManager').")]
        [SerializeField] private string _targetClassType = "";
        [Tooltip("The name of the static C# event to listen to (e.g., 'OnPlayerScored').")]
        [SerializeField] private string _eventName = "";

        private readonly Dictionary<GameObject, Delegate> _subscriptions = new Dictionary<GameObject, Delegate>();

        public override string NodeName => "C# Event Listener";
        public override string Category => "Events";

        public string TargetClassType { get => _targetClassType; set { _targetClassType = value; NotifyChanged(); } }
        public string EventName { get => _eventName; set { _eventName = value; NotifyChanged(); } }
        
        protected override void InitializePorts()
        {
            AddInputPort("subscribe", "▶ Subscribe", FluxPortType.Execution, "void", false);
            AddInputPort("unsubscribe", "▶ Unsubscribe", FluxPortType.Execution, "void", false);
            AddInputPort("context", "Context", FluxPortType.Data, "GameObject", true, null, "Required for safe subscription.");
            
            AddOutputPort("onEvent", "▶ On Event", FluxPortType.Execution, "void", false);
            AddOutputPort("eventArgs", "Arguments", FluxPortType.Data, "object[]", false);
            
            AddOutputPort("onSubscribed", "▶ On Subscribed", FluxPortType.Execution, "void", false);
            AddOutputPort("onUnsubscribed", "▶ On Unsubscribed", FluxPortType.Execution, "void", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var runner = executor.Runner;

            if (inputs.ContainsKey("subscribe"))
            {
                SubscribeToEvent(executor, outputs);
            }
            if (inputs.ContainsKey("unsubscribe"))
            {
                UnsubscribeFromEvent(runner, outputs);
            }
        }

        private void SubscribeToEvent(FluxGraphExecutor executor, Dictionary<string, object> outputs)
        {
            var context = executor.Runner.GetContextObject();
            if (context == null || string.IsNullOrEmpty(_targetClassType) || string.IsNullOrEmpty(_eventName)) return;

            UnsubscribeFromEvent(executor.Runner, null);

            try
            {
                Type targetType = Type.GetType(_targetClassType);
                EventInfo eventInfo = targetType?.GetEvent(_eventName, BindingFlags.Public | BindingFlags.Static);

                if (eventInfo != null)
                {
                    // The handler now captures the executor instance.
                    Action<object[]> handler = (args) => OnEventReceived(executor, args);
                    var genericHandler = CreateMatchingDelegate(eventInfo, handler);
                    
                    eventInfo.AddEventHandler(null, genericHandler);
                    _subscriptions[context] = genericHandler;

                    var cleanup = context.GetComponent<GraphSubscriptionCleanup>() ?? context.AddComponent<GraphSubscriptionCleanup>();
                    cleanup.AddSubscription(new CSharpEventSubscription(targetType, _eventName, genericHandler));
                    
                    if(outputs != null) SetOutputValue(outputs, "onSubscribed", null);
                }
                else
                {
                    Debug.LogError($"EventListenerNode: Could not find static event '{_eventName}' on type '{_targetClassType}'.", this);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventListenerNode: Error subscribing to event: {ex.Message}", this);
            }
        }

        private void UnsubscribeFromEvent(IGraphRunner runner, Dictionary<string, object> outputs)
        {
            var context = runner.GetContextObject();
            if (context == null || !_subscriptions.ContainsKey(context)) return;

            try
            {
                Type targetType = Type.GetType(_targetClassType);
                EventInfo eventInfo = targetType?.GetEvent(_eventName, BindingFlags.Public | BindingFlags.Static);
                if (eventInfo != null && _subscriptions.TryGetValue(context, out var handler))
                {
                    eventInfo.RemoveEventHandler(null, handler);
                }
            }
            finally
            {
                _subscriptions.Remove(context);
                if(outputs != null) SetOutputValue(outputs, "onUnsubscribed", null);
            }
        }
        
        private void OnEventReceived(FluxGraphExecutor executor, object[] args)
        {
            var outputs = new Dictionary<string, object> { { "eventArgs", args } };
            executor.ContinueFromPort(this, "onEvent", outputs);
        }

        // --- Reflection Helper ---
        private Delegate CreateMatchingDelegate(EventInfo eventInfo, Action<object[]> action)
        {
            var handlerType = eventInfo.EventHandlerType;
            var handlerParams = handlerType.GetMethod("Invoke").GetParameters();
            var dynamicMethod = new System.Reflection.Emit.DynamicMethod("", null, handlerParams.Select(p => p.ParameterType).ToArray());
            var il = dynamicMethod.GetILGenerator();

            var argArray = il.DeclareLocal(typeof(object[]));
            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, handlerParams.Length);
            il.Emit(System.Reflection.Emit.OpCodes.Newarr, typeof(object));
            il.Emit(System.Reflection.Emit.OpCodes.Stloc, argArray);

            for (int i = 0; i < handlerParams.Length; i++)
            {
                il.Emit(System.Reflection.Emit.OpCodes.Ldloc, argArray);
                il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, i);
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg, i);
                if (handlerParams[i].ParameterType.IsValueType)
                {
                    il.Emit(System.Reflection.Emit.OpCodes.Box, handlerParams[i].ParameterType);
                }
                il.Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
            }

            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); // 'this' action's target
            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, argArray);
            il.Emit(System.Reflection.Emit.OpCodes.Callvirt, action.GetType().GetMethod("Invoke"));
            il.Emit(System.Reflection.Emit.OpCodes.Ret);

            return dynamicMethod.CreateDelegate(handlerType, action.Target);
        }

        // Helper for cleanup component
        private class CSharpEventSubscription : IDisposable
        {
            private readonly Type _targetType;
            private readonly string _eventName;
            private readonly Delegate _handler;
            public CSharpEventSubscription(Type target, string evt, Delegate handler) { _targetType = target; _eventName = evt; _handler = handler; }
            public void Dispose()
            {
                try
                {
                    var eventInfo = _targetType?.GetEvent(_eventName, BindingFlags.Public | BindingFlags.Static);
                    eventInfo?.RemoveEventHandler(null, _handler);
                }
                catch { /* Fails silently on shutdown */ }
            }
        }
    }
}