using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that interacts with FluxFramework's reactive properties.
    /// It can get, set, check for existence, and safely subscribe/unsubscribe to property changes.
    /// </summary>
    [CreateAssetMenu(fileName = "ReactivePropertyNode", menuName = "Flux/Visual Scripting/Framework/Properties/Reactive Property")]
    public class ReactivePropertyNode : FluxNodeBase
    {
        [Tooltip("The unique key that identifies the reactive property.")]
        [SerializeField] private string _propertyKey = "";

        [Tooltip("The action this node will perform.")]
        [SerializeField] private ReactivePropertyAction _action = ReactivePropertyAction.Get;
        
        [Tooltip("The expected data type of the property. This determines the type of the input/output ports.")]
        [SerializeField] private ReactivePropertyType _propertyType = ReactivePropertyType.Float;
        
        private readonly Dictionary<GameObject, IDisposable> _subscriptions = new Dictionary<GameObject, IDisposable>();

        public override string NodeName => $"Reactive Property ({_action})";
        public override string Category => "Framework/Properties";

        public string PropertyKey { get => _propertyKey; set { _propertyKey = value; NotifyChanged(); } }
        public ReactivePropertyAction Action { get => _action; set { _action = value; RefreshPorts(); } }
        public ReactivePropertyType PropertyType { get => _propertyType; set { _propertyType = value; RefreshPorts(); } }

        protected override void InitializePorts()
        {
            if (_action == ReactivePropertyAction.Set || _action == ReactivePropertyAction.Subscribe || _action == ReactivePropertyAction.Unsubscribe)
            {
                AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            }
            
            AddInputPort("propertyKey", "Property Key", FluxPortType.Data, "string", false, _propertyKey);
            
            if (_action == ReactivePropertyAction.Set)
            {
                AddInputPort("value", "Value", FluxPortType.Data, GetTypeStringForPorts(_propertyType), true);
            }
            
            if (_action == ReactivePropertyAction.Subscribe || _action == ReactivePropertyAction.Unsubscribe)
            {
                // This input is crucial for managing the subscription's lifecycle.
                AddInputPort("context", "Context", FluxPortType.Data, "GameObject", true, null, "The GameObject running this graph. Required for safe subscriptions.");
            }
            
            if (_action == ReactivePropertyAction.Set) AddOutputPort("onSet", "▶ On Set", FluxPortType.Execution, "void", false);
            if (_action == ReactivePropertyAction.Subscribe) AddOutputPort("onSubscribed", "▶ On Subscribed", FluxPortType.Execution, "void", false);
            if (_action == ReactivePropertyAction.Unsubscribe) AddOutputPort("onUnsubscribed", "▶ On Unsubscribed", FluxPortType.Execution, "void", false);
            if (_action == ReactivePropertyAction.Subscribe) AddOutputPort("onChanged", "▶ On Changed", FluxPortType.Execution, "void", false);
            
            if (_action == ReactivePropertyAction.Get || _action == ReactivePropertyAction.Subscribe)
            {
                AddOutputPort("value", "Value", FluxPortType.Data, GetTypeStringForPorts(_propertyType), false);
            }
            AddOutputPort("exists", "Exists", FluxPortType.Data, "bool", false);
        }
        
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            string propertyKey = GetInputValue<string>(inputs, "propertyKey", _propertyKey);
            
            if (string.IsNullOrEmpty(propertyKey) || FluxManager.Instance == null)
            {
                SetOutputValue(outputs, "exists", false);
                return;
            }

            try
            {
                switch (_action)
                {
                    case ReactivePropertyAction.Get:
                        GetPropertyValue(propertyKey, outputs);
                        break;
                    case ReactivePropertyAction.Set:
                        if (inputs.ContainsKey("execute")) SetPropertyValue(propertyKey, inputs, outputs);
                        break;
                    case ReactivePropertyAction.Subscribe:
                        if (inputs.ContainsKey("execute")) SubscribeToProperty(executor, inputs, outputs);
                        break;
                    case ReactivePropertyAction.Unsubscribe:
                        if (inputs.ContainsKey("execute")) UnsubscribeFromProperty(executor, inputs, outputs);
                        break;
                    case ReactivePropertyAction.Exists:
                        CheckPropertyExists(propertyKey, outputs);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ReactivePropertyNode: Error on key '{propertyKey}': {ex.Message}", this);
                SetOutputValue(outputs, "exists", false);
            }
        }
        
        private void GetPropertyValue(string propertyKey, Dictionary<string, object> outputs)
        {
            var property = FluxManager.Instance.GetProperty(propertyKey);
            if (property != null)
            {
                SetOutputValue(outputs, "value", property.GetValue());
                SetOutputValue(outputs, "exists", true);
            }
            else
            {
                SetOutputValue(outputs, "value", GetDefaultValueForType(_propertyType));
                SetOutputValue(outputs, "exists", false);
            }
        }

        private void SetPropertyValue(string propertyKey, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            var property = GetOrCreatePropertyByType(propertyKey);
            if(property == null) return;
            
            object inputValue = GetInputValue<object>(inputs, "value");
            property.SetValue(inputValue);
            
            SetOutputValue(outputs, "onSet", null);
            SetOutputValue(outputs, "exists", true);
        }

        private void SubscribeToProperty(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            GameObject context = GetInputValue<GameObject>(inputs, "context");
            if (context == null)
            {
                Debug.LogError("ReactivePropertyNode: Subscribe action requires a 'Context' GameObject to be connected for safe cleanup.", this);
                if(outputs != null) SetOutputValue(outputs, "exists", false);
                return;
            }
            
            UnsubscribeFromProperty(executor, inputs, null); // Unsubscribe previous for this context

            string propertyKey = GetInputValue<string>(inputs, "propertyKey", _propertyKey);
            var property = GetOrCreatePropertyByType(propertyKey);
            if (property == null)
            {
                if(outputs != null) SetOutputValue(outputs, "exists", false);
                return;
            }

            IDisposable subscription = property.Subscribe(newValue => 
            {
                var eventOutputs = new Dictionary<string, object>
                {
                    { "value", newValue }
                };
                executor.ContinueFromPort(this, "onChanged", eventOutputs);
            });
            
            _subscriptions[context] = subscription;
            
            var cleanup = context.GetComponent<GraphSubscriptionCleanup>() ?? context.AddComponent<GraphSubscriptionCleanup>();
            cleanup.AddSubscription(subscription);

            if(outputs != null)
            {
                SetOutputValue(outputs, "value", property.GetValue());
                SetOutputValue(outputs, "onSubscribed", null);
                SetOutputValue(outputs, "exists", true);
            }
        }

        private void UnsubscribeFromProperty(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            GameObject context = GetInputValue<GameObject>(inputs, "context");
            if (context == null) return;

            if (_subscriptions.TryGetValue(context, out var subscription))
            {
                subscription.Dispose();
                _subscriptions.Remove(context);
                if(outputs != null) SetOutputValue(outputs, "onUnsubscribed", null);
            }
        }
        
        private void CheckPropertyExists(string propertyKey, Dictionary<string, object> outputs)
        {
            bool exists = FluxManager.Instance.HasProperty(propertyKey);
            SetOutputValue(outputs, "exists", exists);
        }
        
        protected void OnDestroy()
        {
            foreach (var sub in _subscriptions.Values) sub.Dispose();
            _subscriptions.Clear();
        }

        private IReactiveProperty GetOrCreatePropertyByType(string key)
        {
            return _propertyType switch
            {
                ReactivePropertyType.Int => FluxManager.Instance.GetOrCreateProperty<int>(key),
                ReactivePropertyType.Float => FluxManager.Instance.GetOrCreateProperty<float>(key),
                ReactivePropertyType.Bool => FluxManager.Instance.GetOrCreateProperty<bool>(key),
                ReactivePropertyType.String => FluxManager.Instance.GetOrCreateProperty<string>(key),
                ReactivePropertyType.Vector3 => FluxManager.Instance.GetOrCreateProperty<Vector3>(key),
                ReactivePropertyType.Color => FluxManager.Instance.GetOrCreateProperty<Color>(key),
                _ => null
            };
        }

        private string GetTypeStringForPorts(ReactivePropertyType type)
        {
            return type switch {
                ReactivePropertyType.Int => "int", ReactivePropertyType.Float => "float",
                ReactivePropertyType.Bool => "bool", ReactivePropertyType.String => "string",
                ReactivePropertyType.Vector3 => "Vector3", ReactivePropertyType.Color => "Color",
                _ => "object"
            };
        }

        private object GetDefaultValueForType(ReactivePropertyType type)
        {
             return type switch {
                ReactivePropertyType.Int => 0, ReactivePropertyType.Float => 0f,
                ReactivePropertyType.Bool => false, ReactivePropertyType.String => "",
                ReactivePropertyType.Vector3 => Vector3.zero, ReactivePropertyType.Color => Color.black,
                _ => null
            };
        }
    }
    
    public enum ReactivePropertyAction { Get, Set, Subscribe, Unsubscribe, Exists }
    public enum ReactivePropertyType { Int, Float, Bool, String, Vector3, Color }

    public class GraphSubscriptionCleanup : MonoBehaviour
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        public void AddSubscription(IDisposable sub) => _subscriptions.Add(sub);
        private void OnDestroy()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }
    }
}