using System.Collections.Generic;
using UnityEngine;
using FluxFramework.UI;
using FluxFramework.Binding;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;
using System;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A Visual Scripting node that triggers the registration or unregistration
    /// of data bindings on a specified FluxUIComponent.
    /// </summary>
    [CreateAssetMenu(fileName = "UIBindingNode", menuName = "Flux/Visual Scripting/Framework/UI/UI Binding")]
    public class UIBindingNode : FluxNodeBase
    {
        [Tooltip("The action this node will perform on the component.")]
        [SerializeField] private UIBindingAction _action = UIBindingAction.Bind;

        public override string NodeName => $"UI Binding ({_action})";
        public override string Category => "Framework/UI";

        public UIBindingAction Action 
        { 
            get => _action; 
            set 
            { 
                _action = value; 
                NotifyChanged();
            } 
        }

        /// <summary>
        /// Defines the input and output ports for this node in the Visual Scripting editor.
        /// </summary>
        protected override void InitializePorts()
        {
            // --- Execution Ports ---
            AddInputPort("execute", "Execute", FluxPortType.Execution, "void", true);
            AddOutputPort("onSuccess", "On Success", FluxPortType.Execution, "void", false);
            AddOutputPort("onFailure", "On Failure", FluxPortType.Execution, "void", false);
            
            // --- Data Ports ---
            AddInputPort("fluxComponent", "Flux Component", FluxPortType.Data, "FluxUIComponent", true);
            AddOutputPort("success", "Success", FluxPortType.Data, "bool", false);
        }

        /// <summary>
        /// The internal execution logic for this node.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (!inputs.ContainsKey("execute"))
            {
                return;
            }
            
            FluxUIComponent fluxComponent = GetInputValue<FluxUIComponent>(inputs, "fluxComponent");

            if (fluxComponent == null)
            {
                Debug.LogError("UIBindingNode: Input Flux Component is null. Make sure to connect a valid component.", this);
                SetOutputValue(outputs, "success", false);
                SetOutputValue(outputs, "onFailure", null); // Trigger failure path
                return;
            }

            try
            {
                switch (_action)
                {
                    case UIBindingAction.Bind:
                        fluxComponent.Bind();
                        Debug.Log($"UIBindingNode: Triggered Bind for component '{fluxComponent.name}'.", this);
                        break;
                    
                    case UIBindingAction.Unbind:
                        fluxComponent.Unbind();
                        Debug.Log($"UIBindingNode: Triggered Unbind for component '{fluxComponent.name}'.", this);
                        break;
                }

                SetOutputValue(outputs, "success", true);
                SetOutputValue(outputs, "onSuccess", null); // Trigger success path
            }
            catch (Exception ex)
            {
                Debug.LogError($"UIBindingNode: Error executing '{_action}' on component '{fluxComponent.name}': {ex.Message}", this);
                SetOutputValue(outputs, "success", false);
                SetOutputValue(outputs, "onFailure", null); // Trigger failure path
            }
        }
    }

    /// <summary>
    /// Defines the actions the UIBindingNode can perform.
    /// </summary>
    public enum UIBindingAction
    {
        /// <summary>
        /// Calls the public RegisterBindings() method on the component.
        /// </summary>
        Bind,
        /// <summary>
        /// Calls the public UnregisterBindings() method on the component.
        /// </summary>
        Unbind
    }
}