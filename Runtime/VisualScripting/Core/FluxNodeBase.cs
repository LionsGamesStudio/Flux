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

        public string NodeId 
        { 
            get 
            { 
                if (string.IsNullOrEmpty(_nodeId))
                    _nodeId = Guid.NewGuid().ToString();
                return _nodeId;
            }
        }

        public virtual string NodeName => !string.IsNullOrEmpty(_customDisplayName) ? _customDisplayName : DefaultNodeName;
        public virtual string DefaultNodeName => _nodeName ?? GetType().Name;
        public string CustomDisplayName { get => _customDisplayName; set { _customDisplayName = value; NotifyChanged(); } }
        public virtual string Category => _category ?? "General";
        public Vector2 Position { get => _position; set { _position = value; } }
        
        public virtual Type GetEditorViewType() => null;
        public IReadOnlyList<FluxNodePort> InputPorts => _inputPorts?.AsReadOnly() ?? new List<FluxNodePort>().AsReadOnly();
        public IReadOnlyList<FluxNodePort> OutputPorts => _outputPorts?.AsReadOnly() ?? new List<FluxNodePort>().AsReadOnly();
        public virtual bool CanExecute => true;

        public event Action<IFluxNode> OnExecuted;
        public event Action<IFluxNode> OnChanged;

        protected virtual void OnEnable()
        {
            if (_inputPorts == null) _inputPorts = new List<FluxNodePort>();
            if (_outputPorts == null) _outputPorts = new List<FluxNodePort>();
            
            if (_inputPorts.Count == 0 && _outputPorts.Count == 0)
            {
                InitializePorts();
            }
        }
        
        protected virtual void InitializePorts() { }

        protected void AddInputPort(string name, string displayName, FluxPortType portType, 
                                   string valueType, bool isRequired = false, object defaultValue = null, string tooltip = null)
        {
            if (_inputPorts == null) _inputPorts = new List<FluxNodePort>();
            if (_inputPorts.Any(p => p.Name == name)) return;

            var port = new FluxNodePort(name, displayName, portType, FluxPortDirection.Input, 
                                       valueType, isRequired, defaultValue, tooltip);
            _inputPorts.Add(port);
        }

        protected void AddOutputPort(string name, string displayName, FluxPortType portType, 
                                    string valueType, bool isRequired = false, string tooltip = null)
        {
            if (_outputPorts == null) _outputPorts = new List<FluxNodePort>();
            if (_outputPorts.Any(p => p.Name == name)) return;

            var port = new FluxNodePort(name, displayName, portType, FluxPortDirection.Output,
                                       valueType, isRequired, null, tooltip);
            _outputPorts.Add(port);
        }
        
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

        protected abstract void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs);
        
        public virtual void RefreshPorts()
        {
            InitializePorts();
            NotifyChanged();
        }

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
        /// This will discard all existing ports and connections. Use with caution.
        /// This is primarily a debugging tool for when a node's port configuration
        /// becomes corrupted or needs a hard reset.
        /// </summary>
        public void ForceRegeneratePorts()
        {
            // Clear the existing port lists completely.
            _inputPorts.Clear();
            _outputPorts.Clear();
            
            // Re-run the initialization logic from scratch.
            InitializePorts();
            
            // Notify the editor that the node has changed significantly.
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

            // This is a simplified lookup. A more robust system might handle fully qualified type names.
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

        // Helper to resolve common type names that Type.GetType struggles with (e.g., "int", "float")
        private Type ResolveTypeName(string typeName)
        {
            return typeName.ToLower() switch
            {
                "int" => typeof(int),
                "float" => typeof(float),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "gameobject" => typeof(GameObject),
                "object" => typeof(object),
                "void" => typeof(void),
                _ => null, // Or search all assemblies if needed
            };
        }

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

        protected void SetOutputValue(Dictionary<string, object> outputs, string portName, object value)
        {
            outputs[portName] = value;
        }

        protected void NotifyChanged()
        {
            OnChanged?.Invoke(this);
            #if UNITY_EDITOR
            if (this != null) EditorUtility.SetDirty(this);
            #endif
        }
        
        protected void NotifyExecuted()
        {
            OnExecuted?.Invoke(this);
        }

        private void OnValidate()
        {
            // The logic to auto-update ports on code change was moved to the custom editor (FluxGraphView)
            // as OnValidate can be unreliable and slow for this purpose.
        }
    }
}