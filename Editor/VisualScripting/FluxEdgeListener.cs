using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// This class implements the IEdgeConnectorListener interface to provide custom behavior
    /// when creating connections. Its main purpose is to handle the case where a user
    /// drops an edge in an empty space to open the node search window.
    /// </summary>
    public class FluxEdgeListener : IEdgeConnectorListener
    {
        private readonly FluxGraphView _graphView;
        private readonly FluxSearchWindowProvider _searchProvider;

        public FluxEdgeListener(FluxGraphView graphView, FluxSearchWindowProvider searchProvider)
        {
            _graphView = graphView;
            _searchProvider = searchProvider;
        }

        /// <summary>
        /// This is called when a user successfully drops an edge onto a valid port.
        /// We use this to ensure the connection is properly registered in our data model.
        /// </summary>
        /// <param name="graphView"></param>
        /// <param name="edge"></param>
        public void OnDrop(GraphView graphView, Edge edge)
        {
            
        }

        /// <summary>
        /// This is called when a user drags an edge and drops it outside of any valid port.
        /// We use this to open our node search window, allowing the user to create a new node.
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="position"></param>
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            // The port the connection was dragged FROM.
            var originPort = (edge.output != null) ? edge.output : edge.input;
            if (originPort == null) return;

            // We re-initialize our existing search provider with the context of the origin port.
            _searchProvider.Initialize(_graphView, EditorWindow.GetWindow<FluxVisualScriptingWindow>(), originPort);

            // And we open the search window at the current mouse position.
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(position)), _searchProvider);
        }
    }
}