using System;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// Associates a custom node view class (inheriting from FluxNodeView)
    /// with a specific node logic class (inheriting from INode).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CustomNodeViewAttribute : Attribute
    {
        public Type NodeType { get; }

        public CustomNodeViewAttribute(Type nodeType)
        {
            NodeType = nodeType;
        }
    }
}