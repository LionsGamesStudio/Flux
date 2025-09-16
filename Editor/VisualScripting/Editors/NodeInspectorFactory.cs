using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    public static class NodeInspectorFactory
    {
        // Maps a node logic Type to the MethodInfo of its custom OnInspectorGUI method.
        private static Dictionary<Type, MethodInfo> _customInspectorMap;
        
        static NodeInspectorFactory()
        {
            Reload();
        }

        [InitializeOnLoadMethod]
        private static void Reload()
        {
            _customInspectorMap = new Dictionary<Type, MethodInfo>();
            
            var inspectorDrawerTypes = TypeCache.GetTypesWithAttribute<CustomNodeInspectorAttribute>();

            foreach (var drawerType in inspectorDrawerTypes)
            {
                var attr = drawerType.GetCustomAttribute<CustomNodeInspectorAttribute>();
                if (attr != null)
                {
                    // We look for a public static method named "OnInspectorGUI"
                    var drawMethod = drawerType.GetMethod("OnInspectorGUI", BindingFlags.Public | BindingFlags.Static);
                    if (drawMethod != null)
                    {
                        _customInspectorMap[attr.NodeType] = drawMethod;
                    }
                }
            }
        }

        public static MethodInfo GetInspectorDrawer(INode nodeLogic)
        {
            if (nodeLogic == null) return null;
            
            _customInspectorMap.TryGetValue(nodeLogic.GetType(), out var drawerMethod);
            return drawerMethod;
        }
    }
}