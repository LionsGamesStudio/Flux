using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// Base interface for all visual scripting nodes in Flux Framework
    /// </summary>
    public interface IFluxNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        string NodeId { get; }

        /// <summary>
        /// Display name of the node
        /// </summary>
        string NodeName { get; }

        /// <summary>
        /// Category for organizing nodes in the editor
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Position of the node in the graph editor
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Input ports for this node
        /// </summary>
        IReadOnlyList<FluxNodePort> InputPorts { get; }

        /// <summary>
        /// Output ports for this node
        /// </summary>
        IReadOnlyList<FluxNodePort> OutputPorts { get; }

        /// <summary>
        /// Whether this node can be executed
        /// </summary>
        bool CanExecute { get; }

        /// <summary>
        /// Execute this node with the given input values
        /// </summary>
        /// <param name="inputs">Input values from connected nodes</param>
        /// <returns>Output values to pass to connected nodes</returns>
        void Execute(FluxGraphExecutor executor, Dictionary<string, object> inputs, out Dictionary<string, object> outputs);

        /// <summary>
        /// Validate the node configuration
        /// </summary>
        /// <returns>True if the node is valid</returns>
        bool Validate();

        /// <summary>
        /// Get the type of value expected for an input port
        /// </summary>
        /// <param name="portName">Name of the input port</param>
        /// <returns>Expected type</returns>
        Type GetInputType(string portName);

        /// <summary>
        /// Get the type of value provided by an output port
        /// </summary>
        /// <param name="portName">Name of the output port</param>
        /// <returns>Output type</returns>
        Type GetOutputType(string portName);

        /// <summary>
        /// Event raised when the node is executed
        /// </summary>
        event Action<IFluxNode> OnExecuted;

        /// <summary>
        /// Event raised when the node configuration changes
        /// </summary>
        event Action<IFluxNode> OnChanged;
    }
}
