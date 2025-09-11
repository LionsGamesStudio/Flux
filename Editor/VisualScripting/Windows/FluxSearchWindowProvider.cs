using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// Provides search functionality for creating nodes in the visual scripting editor
    /// </summary>
    public class FluxSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private FluxGraphView _graphView;

        public void Initialize(FluxGraphView graphView)
        {
            _graphView = graphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Flux Node"), 0)
            };

            // Get all node types that inherit from FluxNodeBase
            var nodeTypes = TypeCache.GetTypesDerivedFrom<FluxNodeBase>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .OrderBy(t => GetNodeCategory(t))
                .ThenBy(t => GetNodeName(t));

            string currentCategory = "";
            
            foreach (var nodeType in nodeTypes)
            {
                string category = GetNodeCategory(nodeType);
                string nodeName = GetNodeName(nodeType);

                // Add category group if it's different from the current one
                if (category != currentCategory)
                {
                    tree.Add(new SearchTreeGroupEntry(new GUIContent(category), 1));
                    currentCategory = category;
                }

                // Add the node entry
                var entry = new SearchTreeEntry(new GUIContent(nodeName))
                {
                    level = 2,
                    userData = nodeType
                };

                tree.Add(entry);
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (!(SearchTreeEntry.userData is Type nodeType))
                return false;

            // Convert screen position to graph view local position
            var screenMousePosition = context.screenMousePosition;
            var windowPosition = _graphView.Window.position.position;
            var localMousePosition = screenMousePosition - windowPosition;
            
            // Convert to graph coordinates (simplified approach)
            var graphMousePosition = new Vector2(localMousePosition.x, localMousePosition.y);

            _graphView.CreateNode(nodeType, graphMousePosition);
            return true;
        }

        private string GetNodeCategory(Type nodeType)
        {
            // Try to get category from the node instance
            try
            {
                var instance = ScriptableObject.CreateInstance(nodeType) as FluxNodeBase;
                if (instance != null)
                {
                    string category = instance.Category;
                    DestroyImmediate(instance);
                    return string.IsNullOrEmpty(category) ? "General" : category;
                }
            }
            catch
            {
                // Fall back to namespace-based categorization
            }

            // Fall back to namespace-based categorization
            string namespaceName = nodeType.Namespace ?? "";
            if (namespaceName.Contains("Nodes"))
            {
                var parts = namespaceName.Split('.');
                var nodesPart = Array.FindIndex(parts, p => p == "Nodes");
                if (nodesPart >= 0 && nodesPart < parts.Length - 1)
                {
                    return parts[nodesPart + 1];
                }
            }

            return "General";
        }

        private string GetNodeName(Type nodeType)
        {
            // Try to get name from the node instance
            try
            {
                var instance = ScriptableObject.CreateInstance(nodeType) as FluxNodeBase;
                if (instance != null)
                {
                    string name = instance.NodeName;
                    DestroyImmediate(instance);
                    if (!string.IsNullOrEmpty(name) && name != nodeType.Name)
                        return name;
                }
            }
            catch
            {
                // Fall back to type name formatting
            }

            // Fall back to formatting the type name
            return FormatTypeName(nodeType.Name);
        }

        private string FormatTypeName(string typeName)
        {
            // Remove "Node" suffix if present
            if (typeName.EndsWith("Node"))
                typeName = typeName.Substring(0, typeName.Length - 4);

            // Add spaces before capital letters
            string result = "";
            for (int i = 0; i < typeName.Length; i++)
            {
                if (i > 0 && char.IsUpper(typeName[i]) && !char.IsUpper(typeName[i - 1]))
                {
                    result += " ";
                }
                result += typeName[i];
            }

            return result;
        }
    }
}
