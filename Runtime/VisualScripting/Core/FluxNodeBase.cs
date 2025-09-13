using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// The abstract base class for all nodes in the Flux Visual Scripting system.
    /// It handles serialization, port management, validation, and the execution contract.
    /// </summary>
    [Serializable]
    public abstract class FluxNodeBase : ScriptableObject, IFluxNode
    {
        [SerializeField] protected string _nodeId;
        [SerializeField] protected string _nodeName;
        [SerializeField] protected string _customDisplayName;
        [SerializeField] protected string _category;
        [SerializeField] protected Vector2 _position;
        [SerializeField] protected List<FluxNodePort> _inputPorts;
        [SerializeField] protected List<FluxNodePort> _outputPorts;

        /// <summary>
        /// A unique identifier for this specific node instance.
        /// </summary>
        public string NodeId 
        { 
            get 
            { 
                if (string.IsNullOrEmpty(_nodeId))
                    _nodeId = Guid.NewGuid().ToString();
                return _nodeId;
            }
        }

        /// <summary>
        /// The name displayed on the node in the editor. Uses a custom name if provided.
        /// </summary>
        public virtual string NodeName => !string.IsNullOrEmpty(_customDisplayName) ? _customDisplayName : DefaultNodeName;
        
        /// <summary>
        /// The default name for the node, typically derived from its class name.
        /// </summary>
        public virtual string DefaultNodeName => _nodeName ?? GetType().Name;
        
        /// <summary>
        /// A user-defined name for this specific node instance.
        /// </summary>
        public string CustomDisplayName { get => _customDisplayName; set { _customDisplayName = value; NotifyChanged(); } }
        
        /// <summary>
        /// The category path for this node in the creation menu (e.g., "Logic/Math").
        /// </summary>
        public virtual string Category => _category ?? "General";
        
        /// <summary>
        /// The position of the node on the graph canvas.
        /// </summary>
        public Vector2 Position { get => _position; set { _position = value; } }
        
        /// <summary>
        /// An optional override to specify a custom editor view class for this node.
        /// </summary>
        public virtual Type GetEditorViewType() => null;
        
        /// <summary>
        /// A read-only list of all input ports on this node.
        /// </summary>
        public IReadOnlyList<FluxNodePort> InputPorts => _inputPorts?.AsReadOnly() ?? new List<FluxNodePort>().AsReadOnly();
        
        /// <summary>
        /// A read-only list of all output ports on this node.
        /// </summary>
        public IReadOnlyList<FluxNodePort> OutputPorts => _outputPorts?.AsReadOnly() ?? new List<FluxNodePort>().AsReadOnly();
        
        /// <summary>
        /// Determines if this node can be executed by the graph runner.
        /// </summary>
        public virtual bool CanExecute => true;

        /// <summary>
        /// An event fired immediately after this node has finished executing its logic.
        /// </summary>
        public event Action<IFluxNode> OnExecuted;
        
        /// <summary>
        /// An event fired whenever a property of the node is changed in the editor.
        /// </summary>
        public event Action<IFluxNode> OnChanged;

        /// <summary>
        /// Creates a deep copy of this node instance with a new, unique NodeId.
        /// </summary>
        /// <returns>A new instance of the node with copied values.</returns>
        public virtual FluxNodeBase Clone()
        {
            var clone = Instantiate(this);
            clone._nodeId = Guid.NewGuid().ToString();
            clone.name = this.name;
            return clone;
        }

        /// <summary>
        /// Unity's OnEnable, used here to ensure port lists are initialized.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_inputPorts == null) _inputPorts = new List<FluxNodePort>();
            if (_outputPorts == null) _outputPorts = new List<FluxNodePort>();
            
            if (_inputPorts.Count == 0 && _outputPorts.Count == 0)
            {
                InitializePorts();
            }
        }
        
        /// <summary>
        /// The main method for derived nodes to define their ports. This is called once on creation.
        /// </summary>
        protected virtual void InitializePorts() { }

        /// <summary>
        /// Adds a new input port to this node.
        /// </summary>
        protected void AddInputPort(string name, string displayName, FluxPortType portType, 
                                   string valueType, bool isRequired = false, object defaultValue = null, string tooltip = null)
        {
            if (_inputPorts == null) _inputPorts = new List<FluxNodePort>();
            if (_inputPorts.Any(p => p.Name == name)) return;

            var port = new FluxNodePort(name, displayName, portType, FluxPortDirection.Input, 
                                       valueType, isRequired, defaultValue, tooltip);
            _inputPorts.Add(port);
        }

        /// <summary>
        /// Adds a new output port to this node.
        /// </summary>
        protected void AddOutputPort(string name, string displayName, FluxPortType portType, 
                                    string valueType, bool isRequired = false, string tooltip = null)
        {
            if (_outputPorts == null) _outputPorts = new List<FluxNodePort>();
            if (_outputPorts.Any(p => p.Name == name)) return;

            var port = new FluxNodePort(name, displayName, portType, FluxPortDirection.Output,
                                       valueType, isRequired, null, tooltip);
            _outputPorts.Add(port);
        }
        
        /// <summary>
        /// The public execution entry point called by the FluxGraphExecutor.
        /// </summary>
        public void Execute(FluxGraphExecutor executor, Dictionary<string, object> inputs, out Dictionary<string, object> outputs)
        {
            outputs = new Dictionary<string, object>();
            try
            {
                ExecuteInternal(executor, inputs, outputs);
                OnExecuted?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing node {NodeName} ({GetType().Name}): {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        /// <summary>
        /// The internal execution logic that must be implemented by all derived nodes.
        /// </summary>
        protected abstract void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs);
        
        /// <summary>
        /// Re-initializes the node's ports.
        /// </summary>
        public virtual void RefreshPorts()
        {
            InitializePorts();
            NotifyChanged();
        }

        /// <summary>
        /// Validates the node's state, primarily checking for required but unconnected input ports.
        /// </summary>
        public virtual bool Validate()
        {
            foreach (var port in InputPorts)
            {
                if (port.IsRequired && !port.IsConnected)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Forces a complete and destructive regeneration of the node's ports.
        /// </summary>
        public void ForceRegeneratePorts()
        {
            _inputPorts.Clear();
            _outputPorts.Clear();
            InitializePorts();
            NotifyChanged();
            Debug.Log($"[FluxFramework] Force-regenerated ports for node: {NodeName}", this);
        }
        
        /// <summary>
        /// Gets the System.Type of value expected for a specific input port.
        /// </summary>
        public virtual Type GetInputType(string portName)
        {
            var port = InputPorts.FirstOrDefault(p => p.Name == portName);
            if (port == null) return null;
            return Type.GetType(port.ValueType) ?? ResolveTypeName(port.ValueType);
        }

        /// <summary>
        /// Gets the System.Type of value provided by a specific output port.
        /// </summary>
        public virtual Type GetOutputType(string portName)
        {
            var port = OutputPorts.FirstOrDefault(p => p.Name == portName);
            if (port == null) return null;
            return Type.GetType(port.ValueType) ?? ResolveTypeName(port.ValueType);
        }

        /// <summary>
        /// A helper to resolve common C# type names that Type.GetType struggles with.
        /// </summary>
        private Type ResolveTypeName(string typeName)
        {
            return typeName?.ToLower() switch
            {
                "int" => typeof(int), "float" => typeof(float), "string" => typeof(string),
                "bool" => typeof(bool), "gameobject" => typeof(GameObject), "object" => typeof(object),
                "void" => typeof(void), _ => null,
            };
        }

        /// <summary>
        /// A helper for derived nodes to safely get a value from an input port.
        /// </summary>
        protected T GetInputValue<T>(Dictionary<string, object> inputs, string portName, T defaultValue = default(T))
        {
            if (inputs.TryGetValue(portName, out object value))
            {
                if (value is T typedValue) return typedValue;
                try { return (T)Convert.ChangeType(value, typeof(T)); } catch { /* Conversion failed */ }
            }
            var port = InputPorts.FirstOrDefault(p => p.Name == portName);
            if (port?.DefaultValue is T portDefault) return portDefault;
            return defaultValue;
        }

        /// <summary>
        /// A helper for derived nodes to set an output port's value.
        /// </summary>
        protected void SetOutputValue(Dictionary<string, object> outputs, string portName, object value)
        {
            outputs[portName] = value;
        }

        /// <summary>
        /// Notifies the editor that a property on this node has changed, marking it as dirty.
        /// </summary>
        protected void NotifyChanged()
        {
            OnChanged?.Invoke(this);
            #if UNITY_EDITOR
            if (this != null) EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// Notifies listeners that this node has finished its execution.
        /// </summary>
        protected void NotifyExecuted()
        {
            OnExecuted?.Invoke(this);
        }
    }
}