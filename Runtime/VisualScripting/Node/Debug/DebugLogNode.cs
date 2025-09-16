using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Debug Log", Category = "Debug", Description = "Logs a message to the Unity console.")]
    public class DebugLogNode : IExecutableNode
    {
        // --- Configuration Fields (visible in inspector) ---
        [Tooltip("The type of log message to display.")]
        public LogType logType = LogType.Log;

        [Tooltip("An optional prefix to add before the message.")]
        public string prefix = "[FluxGraph]";

        // --- Input Ports ---
        [Port(FluxPortDirection.Input, "In", portType: FluxPortType.Execution, PortCapacity.Single)]
        public ExecutionPin In;

        [Port(FluxPortDirection.Input, "Message", portType: FluxPortType.Data, PortCapacity.Multi)]
        public object message;

        // --- Output Ports ---
        [Port(FluxPortDirection.Output, "Out", portType: FluxPortType.Execution, PortCapacity.Multi)]
        public ExecutionPin Out;
        
        
        // This is a synchronous node. Its Execute method has a void return type.
        // The executor will automatically continue the flow from its execution outputs.
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            var contextObject = executor.Runner.GetContextObject();
            string finalMessage = $"{prefix} {message?.ToString() ?? "null"}";

            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(finalMessage, contextObject);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(finalMessage, contextObject);
                    break;
                case LogType.Error:
                    Debug.LogError(finalMessage, contextObject);
                    break;
            }
        }
    }
}