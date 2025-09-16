using System;
using System.Collections.Generic;
using FluxFramework.Attributes.VisualScripting;
using UnityEngine;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Get Transform", Category = "GameObject", Description = "Gets the Transform component from a GameObject.")]
    public class GetTransformNode : IExecutableNode
    {
        // --- Input Port ---
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] 
        public GameObject Target;

        // --- Output Port ---
        [Port(FluxPortDirection.Output, "Transform", PortCapacity.Multi)] 
        public Transform Transform;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (Target != null)
            {
                Transform = Target.transform;
            }
            else
            {
                Transform = null;
            }
        }
    }
}