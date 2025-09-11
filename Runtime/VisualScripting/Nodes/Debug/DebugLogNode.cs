using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that logs a message to the Unity console.
    /// Essential for debugging visual script graphs.
    /// </summary>
    [CreateAssetMenu(fileName = "DebugLogNode", menuName = "Flux/Visual Scripting/Debug/Debug Log")]
    public class DebugLogNode : FluxNodeBase
    {
        [Tooltip("The type of log message to display (Log, Warning, Error).")]
        [SerializeField] private LogType _logType = LogType.Log;
        [Tooltip("An optional prefix to add before the log message.")]
        [SerializeField] private string _prefix = "";

        public override string NodeName => "Debug Log";
        public override string Category => "Debug";

        public LogType LogType 
        { 
            get => _logType; 
            set { _logType = value; NotifyChanged(); } 
        }

        public string Prefix 
        { 
            get => _prefix; 
            set { _prefix = value; NotifyChanged(); } 
        }

        protected override void InitializePorts()
        {
            // Execution flow
            AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
            AddOutputPort("completed", "▶ Out", FluxPortType.Execution, "void", false);
            
            // Data inputs
            AddInputPort("message", "Message", FluxPortType.Data, "object", true, "Hello from Flux!");
            
            // The context object to highlight when the log is clicked in the console.
            AddInputPort("context", "Context", FluxPortType.Data, "Object", false, null, "Optional. If null, the graph runner's GameObject will be used.");
        }

        /// <summary>
        /// Executes the logging action.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            // Only run if the execution port is triggered.
            if (!inputs.ContainsKey("execute"))
            {
                return;
            }

            // Get the message from the input port.
            object message = GetInputValue<object>(inputs, "message");

            // Get the context object. Use the graph executor's context as a fallback.
            Object context = GetInputValue<Object>(inputs, "context") ?? executor.Runner.GetContextObject();

            string finalMessage = string.IsNullOrEmpty(_prefix) 
                ? message?.ToString() ?? "null"
                : $"{_prefix}: {message?.ToString() ?? "null"}";

            switch (_logType)
            {
                case LogType.Log:
                    Debug.Log(finalMessage, context);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(finalMessage, context);
                    break;
                case LogType.Error:
                    Debug.LogError(finalMessage, context);
                    break;
                // Assert and Exception are less common for a simple log node, but can be kept.
                case LogType.Assert:
                    Debug.LogAssertion(finalMessage, context);
                    break;
                case LogType.Exception:
                    // It's generally better to throw a real exception, but logging an error is safer in a graph.
                    Debug.LogError($"[EXCEPTION THROWN FROM GRAPH] {finalMessage}", context);
                    break;
            }

            // Continue the execution flow.
            SetOutputValue(outputs, "completed", null);
        }
    }
}