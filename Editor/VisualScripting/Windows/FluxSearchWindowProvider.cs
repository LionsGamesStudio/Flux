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
        
        // This will hold the port the user is dragging from. It will be null
        // if the user is just right-clicking on the canvas.
        private Port _originPort;

        /// <summary>
        /// Initializes the provider with the necessary context.
        /// </summary>
        /// <param name="graphView">The target graph view.</param>
        /// <param name="window">The parent editor window.</param>
        /// <param name="originPort">The port from which a connection is being dragged, if any.</param>
        public void Initialize(FluxGraphView graphView, EditorWindow window, Port originPort = null)
        {
            _graphView = graphView;
            _window = window;
            _originPort = originPort;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
            };

            // Get all valid INode types with the [FluxNode] attribute.
            var nodeTypes = TypeCache.GetTypesWithAttribute<FluxNodeAttribute>()
                .Where(t => typeof(INode).IsAssignableFrom(t) && !t.IsAbstract);

            // If we are dragging from a port, we can filter this list to only show compatible nodes.
            // (This is an advanced feature we can add later. For now, we show all nodes).
            
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
            
            var createdGroups = new HashSet<string>();
            foreach (var (categoryPath, types) in sortedCategories)
            {
                var pathParts = categoryPath.Split('/');
                var cumulativePath = string.Empty;

                for (var i = 0; i < pathParts.Length; i++)
                {
                    cumulativePath = string.Join("/", pathParts.Take(i + 1));
                    if (createdGroups.Add(cumulativePath))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(pathParts[i]), i + 1));
                    }
                }
                
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

        /// <summary>
        /// Called when the user selects a node type from the search window.
        /// </summary>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is Type nodeLogicType)
            {
                // We call the single, unified method on the GraphView.
                // It will handle both cases (originPort is null or not null) correctly.
                _graphView.CreateNodeAndConnect(nodeLogicType, context.screenMousePosition, _originPort);
                return true;
            }
            return false;
        }
    }
}