using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that gets or sets properties of a UI Text component (Legacy or TextMeshPro).
    /// </summary>
    [CreateAssetMenu(fileName = "UITextNode", menuName = "Flux/Visual Scripting/UI/UI Text")]
    public class UITextNode : FluxNodeBase
    {
        [Tooltip("The action to perform on the text component.")]
        [SerializeField] private UITextAction _action = UITextAction.SetText;

        public override string NodeName => $"UI Text ({_action})";
        public override string Category => "UI";

        public UITextAction Action 
        { 
            get => _action; 
            set { _action = value; RefreshPorts(); } 
        }

        protected override void InitializePorts()
        {
            // --- DYNAMIC PORTS based on Action ---
            
            if (_action == UITextAction.SetText)
            {
                // Execution ports for setting text
                AddInputPort("execute", "▶ In", FluxPortType.Execution, "void", true);
                AddOutputPort("onSet", "▶ Out", FluxPortType.Execution, "void", false);
            }
            
            // Data ports
            AddInputPort("target", "Target Text", FluxPortType.Data, "Component", true, null, "The Text or TextMeshProUGUI component.");

            if (_action == UITextAction.SetText)
            {
                AddInputPort("text", "Text", FluxPortType.Data, "string", true);
            }

            AddOutputPort("text", "Text", FluxPortType.Data, "string", false);
        }

        /// <summary>
        /// Executes the Get or Set action on the text component.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            if (_action == UITextAction.SetText && !inputs.ContainsKey("execute")) return;
            
            Component target = GetInputValue<Component>(inputs, "target");
            if (target == null)
            {
                Debug.LogWarning("UITextNode: Target Text component is null.", this);
                return;
            }

            // --- UNIFIED LOGIC for Text and TextMeshPro ---
            
            // Try to get the component's text interface
            if (!TryGetTextAdapter(target, out var textAdapter))
            {
                Debug.LogError($"UITextNode: The provided target '{target.name}' is not a valid Text or TextMeshProUGUI component.", this);
                return;
            }
            
            if (_action == UITextAction.SetText)
            {
                string textToSet = GetInputValue<string>(inputs, "text");
                textAdapter.SetText(textToSet);
                SetOutputValue(outputs, "onSet", null);
            }

            // Always output the current text
            SetOutputValue(outputs, "text", textAdapter.GetText());
        }

        /// <summary>
        /// A private helper to abstract away the difference between Text and TextMeshProUGUI.
        /// This is an example of the "Adapter" design pattern.
        /// </summary>
        private bool TryGetTextAdapter(Component component, out ITextAdapter adapter)
        {
            adapter = null;
            if (component is TMP_Text tmp)
            {
                adapter = new TmpTextAdapter(tmp);
                return true;
            }
            if (component is UnityEngine.UI.Text legacyText)
            {
                adapter = new LegacyTextAdapter(legacyText);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Defines the actions the UITextNode can perform.
    /// </summary>
    public enum UITextAction
    {
        SetText,
        GetText
    }

    // --- ADAPTER PATTERN for handling different text components ---

    /// <summary>
    /// An interface that provides a common way to interact with different text components.
    /// </summary>
    internal interface ITextAdapter
    {
        string GetText();
        void SetText(string text);
    }

    /// <summary>
    /// Adapter for the modern TextMeshProUGUI component.
    /// </summary>
    internal class TmpTextAdapter : ITextAdapter
    {
        private readonly TMP_Text _component;
        public TmpTextAdapter(TMP_Text component) { _component = component; }
        public string GetText() => _component.text;
        public void SetText(string text) => _component.text = text;
    }
    
    /// <summary>
    /// Adapter for the legacy UnityEngine.UI.Text component.
    /// </summary>
    internal class LegacyTextAdapter : ITextAdapter
    {
        private readonly UnityEngine.UI.Text _component;
        public LegacyTextAdapter(UnityEngine.UI.Text component) { _component = component; }
        public string GetText() => _component.text;
        public void SetText(string text) => _component.text = text;
    }
}