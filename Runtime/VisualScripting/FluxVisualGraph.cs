using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.VisualScripting
{
    /// <summary>
    /// The main ScriptableObject asset that represents a complete visual script.
    /// It acts as the central data container for all nodes and connections.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFluxGraph", menuName = "Flux/Visual Scripting/New Graph")]
    public class FluxVisualGraph : ScriptableObject
    {
        [SerializeReference] // Use SerializeReference to handle derived types like AttributedNodeWrapper
        private List<FluxNodeBase> _nodes = new List<FluxNodeBase>();
        
        [SerializeField]
        private List<FluxNodeConnection> _connections = new List<FluxNodeConnection>();

        public IReadOnlyList<FluxNodeBase> Nodes => _nodes;
        public IReadOnlyList<FluxNodeConnection> Connections => _connections;

        #if UNITY_EDITOR
        /// <summary>
        /// (Editor-only) Creates a new node, adds it to this graph asset, and returns it.
        /// </summary>
        public T CreateNode<T>(Vector2 position) where T : FluxNodeBase
        {
            var node = ScriptableObject.CreateInstance<T>();
            node.name = typeof(T).Name;
            node.Position = position;
            
            // Important: This makes the node a sub-asset of the graph, keeping the project clean.
            UnityEditor.AssetDatabase.AddObjectToAsset(node, this); 
            
            _nodes.Add(node);
            UnityEditor.EditorUtility.SetDirty(this);
            return node;
        }

        /// <summary>
        /// (Editor-only) Deletes a node and all connections attached to it.
        /// </summary>
        public void DeleteNode(FluxNodeBase node)
        {
            if (node == null || !_nodes.Contains(node)) return;

            // Remove connections first
            _connections.RemoveAll(conn => conn.FromNodeId == node.NodeId || conn.ToNodeId == node.NodeId);
            
            _nodes.Remove(node);
            
            UnityEditor.AssetDatabase.RemoveObjectFromAsset(node);
            Object.DestroyImmediate(node, true); // Destroy the sub-asset
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// (Editor-only) Creates a connection between two ports.
        /// </summary>
        public void AddConnection(FluxNodePort fromPort, FluxNodeBase fromNode, FluxNodePort toPort, FluxNodeBase toNode)
        {
            var newConnection = new FluxNodeConnection(fromNode.NodeId, fromPort.Name, toNode.NodeId, toPort.Name);
            _connections.Add(newConnection);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// (Editor-only) Removes a connection.
        /// </summary>
        public void RemoveConnection(FluxNodeConnection connection)
        {
            _connections.Remove(connection);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}