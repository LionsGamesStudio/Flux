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

            var nodeTypes = TypeCache.GetTypesWithAttribute<FluxNodeAttribute>()
                .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsAbstract);

            // Use a sorted dictionary to automatically handle alphabetical sorting of categories.
            var sortedCategories = new SortedDictionary<string, List<Type>>();
            foreach (var type in nodeTypes)
            {
                var attr = type.GetCustomAttribute<FluxNodeAttribute>();
                var category = string.IsNullOrEmpty(attr.Category) ? "General" : attr.Category;
                if (!sortedCategories.ContainsKey(category))
                {
                    sortedCategories[category] = new List<Type>();
                }
                sortedCategories[category].Add(type);
            }
            
            // This set helps create group entries only once.
            var createdGroups = new HashSet<string>();

            foreach (var (categoryPath, types) in sortedCategories)
            {
                var pathParts = categoryPath.Split('/');
                var cumulativePath = string.Empty;

                // Create group entries for the category path (e.g., "Math", then "Math/Advanced")
                for (var i = 0; i < pathParts.Length; i++)
                {
                    cumulativePath = string.Join("/", pathParts.Take(i + 1));
                    if (createdGroups.Add(cumulativePath))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(pathParts[i]), i + 1));
                    }
                }
                
                // Add the actual node types to the tree, sorted alphabetically by display name.
                foreach (var type in types.OrderBy(t => t.GetCustomAttribute<FluxNodeAttribute>().DisplayName))
                {
                    var attr = type.GetCustomAttribute<FluxNodeAttribute>();
                    tree.Add(new SearchTreeEntry(new GUIContent(attr.DisplayName))
                    {
                        userData = type,
                        level = pathParts.Length + 1
                    });
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