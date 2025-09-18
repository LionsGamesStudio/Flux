using System;

namespace FluxFramework.Testing.Attributes
{
    /// <summary>
    /// Marks a method as an executable test case for the Flux Test Runner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class FluxTestAttribute : Attribute { }
}