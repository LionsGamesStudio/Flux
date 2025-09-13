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
    /// Provides hierarchical search functionality for creating nodes in the visual scripting editor.
    /// </summary>
    public class FluxSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private FluxGraphView _graphView;
        private FluxVisualScriptingWindow _window;

        public void Initialize(FluxGraphView graphView, FluxVisualScriptingWindow window)
        {
            _graphView = graphView;
            _window = window;
        }

        /// <summary>
        /// Creates the hierarchical search tree for node creation.
        /// </summary>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            var nodeTypes = TypeCache.GetTypesDerivedFrom<FluxNodeBase>()
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition);

            // --- HIERARCHICAL SORTING LOGIC ---

            // 1. Group all node types by their full category path.
            // Using a SortedDictionary ensures that top-level categories are alphabetical.
            var nodesByCategory = new SortedDictionary<string, List<Type>>();
            foreach (var type in nodeTypes)
            {
                var instance = CreateInstance(type) as FluxNodeBase;
                if (instance != null)
                {
                    string category = string.IsNullOrEmpty(instance.Category) ? "General" : instance.Category;
                    if (!nodesByCategory.ContainsKey(category))
                    {
                        nodesByCategory[category] = new List<Type>();
                    }
                    nodesByCategory[category].Add(type);
                    DestroyImmediate(instance);
                }
            }

            // 2. We now build the tree in a single pass.
            // This ensures that nodes are added immediately after their parent groups.
            var createdGroups = new HashSet<string>();
            foreach (var categoryEntry in nodesByCategory)
            {
                var categoryPath = categoryEntry.Key;
                var typesInCat = categoryEntry.Value;
                var pathParts = categoryPath.Split('/');

                // 2a. Create the folder structure for the current category if it doesn't exist yet.
                var cumulativePath = "";
                for (int i = 0; i < pathParts.Length; i++)
                {
                    cumulativePath = string.Join("/", pathParts.Take(i + 1));
                    if (!createdGroups.Contains(cumulativePath))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(pathParts[i]), i + 1));
                        createdGroups.Add(cumulativePath);
                    }
                }

                // 2b. Add the nodes for this category immediately after creating its folder structure.
                int indentLevel = pathParts.Length + 1;
                var sortedTypes = typesInCat.OrderBy(t => GetNodeName(t));
                foreach (var type in sortedTypes)
                {
                    var entry = new SearchTreeEntry(new GUIContent(GetNodeName(type)))
                    {
                        level = indentLevel,
                        userData = type
                    };
                    tree.Add(entry);
                }
            }

            return tree;
        }

        /// <summary>
        /// Called when the user selects an entry from the search window.
        /// </summary>
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is Type nodeType)
            {
                _graphView.CreateNodeAtScreenPosition(nodeType, context.screenMousePosition);
                return true;
            }
            return false;
        }

        #region Helper Methods

        private string GetNodeName(Type nodeType)
        {
            var instance = CreateInstance(nodeType) as FluxNodeBase;
            if (instance != null)
            {
                string name = instance.NodeName;
                DestroyImmediate(instance);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            return ObjectNames.NicifyVariableName(nodeType.Name.Replace("Node", ""));
        }
        
        #endregion
    }
}