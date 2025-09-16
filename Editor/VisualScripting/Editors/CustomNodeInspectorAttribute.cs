using System;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// Associates a custom inspector drawing class with a specific node logic class (INode).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CustomNodeInspectorAttribute : Attribute
    {
        public Type NodeType { get; }

        public CustomNodeInspectorAttribute(Type nodeType)
        {
            NodeType = nodeType;
        }
    }
}