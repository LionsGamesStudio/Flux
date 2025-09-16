using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    public static class NodeViewFactory
    {
        private static Dictionary<Type, Type> _customViewMap;

        static NodeViewFactory()
        {
            Reload();
        }

        [InitializeOnLoadMethod]
        private static void Reload()
        {
            _customViewMap = new Dictionary<Type, Type>();
            
            // Find all classes with the [CustomNodeView] attribute
            var customViewTypes = TypeCache.GetTypesWithAttribute<CustomNodeViewAttribute>();

            foreach (var viewType in customViewTypes)
            {
                var attr = viewType.GetCustomAttributes(typeof(CustomNodeViewAttribute), false)[0] as CustomNodeViewAttribute;
                if (attr != null)
                {
                    // Map the node logic type to the view type
                    _customViewMap[attr.NodeType] = viewType;
                }
            }
        }

        public static FluxNodeView CreateNodeView(FluxNodeBase nodeData, FluxGraphView graphView)
        {
            if (nodeData is AttributedNodeWrapper wrapper && wrapper.NodeLogic != null)
            {
                var logicType = wrapper.NodeLogic.GetType();
                
                // Check if we have a custom view registered for this logic type
                if (_customViewMap.TryGetValue(logicType, out var viewType))
                {
                    try
                    {
                        // Create an instance of the custom view
                        return (FluxNodeView)Activator.CreateInstance(viewType, wrapper, graphView);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"Failed to create custom node view '{viewType.Name}' for node type '{logicType.Name}'. Falling back to default. Error: {e.Message}");
                    }
                }
            }

            // If no custom view is found, create the default one.
            return new FluxNodeView(nodeData, graphView);
        }
    }
}