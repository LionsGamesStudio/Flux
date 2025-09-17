using UnityEngine;
using TMPro;

namespace FluxFramework.VisualScripting.Node
{
    /// <summary>
    /// An interface that provides a common way to interact with different text components.
    /// This is the core of the Adapter design pattern.
    /// </summary>
    public interface ITextAdapter
    {
        string GetText();
        void SetText(string text);
    }

    /// <summary>
    /// A static helper class to get the correct text adapter for a given component,
    /// allowing unified logic for both legacy UI.Text and TextMeshPro.
    /// </summary>
    public static class UITextAdapterHelper
    {
        /// <summary>
        /// Attempts to create a text adapter for the given component.
        /// </summary>
        /// <param name="component">The component to adapt (must be Text or TextMeshProUGUI).</param>
        /// <param name="adapter">The created adapter if successful.</param>
        /// <returns>True if the component is a supported text type, otherwise false.</returns>
        public static bool TryGetAdapter(Component component, out ITextAdapter adapter)
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

        // --- Private Adapter Implementations ---

        /// <summary>
        /// Adapter for the modern TextMeshProUGUI component.
        /// </summary>
        private class TmpTextAdapter : ITextAdapter
        {
            private readonly TMP_Text _component;
            public TmpTextAdapter(TMP_Text component) { _component = component; }
            public string GetText() => _component.text;
            public void SetText(string text) => _component.text = text;
        }

        /// <summary>
        /// Adapter for the legacy UnityEngine.UI.Text component.
        /// </summary>
        private class LegacyTextAdapter : ITextAdapter
        {
            private readonly UnityEngine.UI.Text _component;
            public LegacyTextAdapter(UnityEngine.UI.Text component) { _component = component; }
            public string GetText() => _component.text;
            public void SetText(string text) => _component.text = text;
        }
    }
}