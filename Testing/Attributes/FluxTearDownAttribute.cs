using System;

namespace FluxFramework.Testing.Attributes
{
    /// <summary>
    /// Marks a method to be executed AFTER each test ([FluxTest]) within the same class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class FluxTearDownAttribute : Attribute { }
}