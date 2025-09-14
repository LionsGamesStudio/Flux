using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private FluxGraphView _graphView;
        private EditorWindow _window;

        public void Initialize(FluxGraphView graphView, EditorWindow window)
        {
            _graphView = graphView;
            _window = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            // Use TypeCache to find all INode classes with our custom attribute, which is very fast.
            var nodeTypes = TypeCache.GetTypesWithAttribute<FluxNodeAttribute>()
                .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsAbstract);

            // Create a sorted dictionary to build the hierarchy
            var sortedNodes = new SortedDictionary<string, List<Type>>();
            foreach (var type in nodeTypes)
            {
                var nodeAttr = type.GetCustomAttribute<FluxNodeAttribute>();
                var category = string.IsNullOrEmpty(nodeAttr.Category) ? "General" : nodeAttr.Category;

                if (!sortedNodes.ContainsKey(category))
                {
                    sortedNodes[category] = new List<Type>();
                }
                sortedNodes[category].Add(type);
            }
            
            // Add categories to the tree
            var groups = new HashSet<string>();
            foreach (var (categoryPath, types) in sortedNodes)
            {
                var pathParts = categoryPath.Split('/');
                var cumulativePath = string.Empty;

                for (int i = 0; i < pathParts.Length; i++)
                {
                    cumulativePath += pathParts[i];
                    if (!groups.Contains(cumulativePath))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(pathParts[i]), i + 1));
                        groups.Add(cumulativePath);
                    }
                }
                
                // Add node entries under their category
                foreach (var type in types.OrderBy(t => t.GetCustomAttribute<FluxNodeAttribute>().DisplayName))
                {
                    var nodeAttr = type.GetCustomAttribute<FluxNodeAttribute>();
                    var entry = new SearchTreeEntry(new GUIContent(nodeAttr.DisplayName))
                    {
                        userData = type,
                        level = pathParts.Length + 1
                    };
                    tree.Add(entry);
                }
            }
            
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is Type nodeLogicType)
            {
                _graphView.CreateAttributedNode(nodeLogicType, context.screenMousePosition);
                return true;
            }
            return false;
        }
    }
}