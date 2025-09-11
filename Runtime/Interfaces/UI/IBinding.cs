using System;
using UnityEngine;

namespace FluxFramework.Binding
{
    /// <summary>
    /// Interface for reactive bindings
    /// </summary>
    public interface IBinding : IDisposable
    {
        /// <summary>
        /// The component this binding is attached to
        /// </summary>
        Component Component { get; }

        /// <summary>
        /// The property key this binding is connected to
        /// </summary>
        string PropertyKey { get; }

        /// <summary>
        /// Updates the UI component from the bound property
        /// </summary>
        void UpdateFromProperty();

        /// <summary>
        /// Updates the bound property from the UI component
        /// </summary>
        void UpdateToProperty();

        /// <summary>
        /// Whether this binding is currently active
        /// </summary>
        bool IsActive { get; }
    }

    /// <summary>
    /// Generic interface for typed UI bindings
    /// </summary>
    /// <typeparam name="T">Type of the bound value</typeparam>
    public interface IUIBinding<T> : IUIBinding
    {
        /// <summary>
        /// Updates the UI with the new value
        /// </summary>
        /// <param name="value">New value to display</param>
        void UpdateUI(T value);

        /// <summary>
        /// Gets the current value from the UI
        /// </summary>
        /// <returns>Current UI value</returns>
        T GetUIValue();
    }

    /// <summary>
    /// Non-generic interface for UI bindings
    /// </summary>
    public interface IUIBinding : IBinding
    {
        /// <summary>
        /// Updates the UI with the new value
        /// </summary>
        /// <param name="value">New value to display</param>
        void UpdateUI(object value);

        /// <summary>
        /// Gets the current value from the UI
        /// </summary>
        /// <returns>Current UI value</returns>
        object GetUIValue();

        /// <summary>
        /// The System.Type of the data this binding handles (e.g., typeof(string), typeof(float)).
        /// </summary>
        Type ValueType { get; }
    }
}
