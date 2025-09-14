using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace FluxFramework.VisualScripting.Editor
{
    public class FluxNodeView : Node
    {
        public FluxNodeBase Node { get; }

        public FluxNodeView(FluxNodeBase node) : base()
        {
            this.Node = node;
            this.title = node.name;
            this.viewDataKey = node.NodeId; // This is used by Unity to save/load the layout

            style.left = node.Position.x;
            style.top = node.Position.y;

            CreateInputPorts();
            CreateOutputPorts();
        }

        private void CreateInputPorts()
        {
            foreach (var portData in Node.InputPorts)
            {
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, GetPortType(portData));
                portView.portName = portData.Name;
                portView.name = portData.Name; // Used for querying
                inputContainer.Add(portView);
            }
        }
        
        private void CreateOutputPorts()
        {
            foreach (var portData in Node.OutputPorts)
            {
                // Output ports can have multiple connections
                var portView = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, GetPortType(portData));
                portView.portName = portData.Name;
                portView.name = portData.Name; // Used for querying
                outputContainer.Add(portView);
            }
        }

        private System.Type GetPortType(FluxNodePort portData)
        {
            return System.Type.GetType(portData.ValueTypeName) ?? typeof(object);
        }
        
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.Position = newPos.position;
        }
    }
}