using System;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Rotation", Category = "Transform", Description = "Gets the world or local rotation of a Transform.")]
    public class GetRotationNode : IExecutableNode
    {
        public Space space = Space.World;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Output, "Euler Angles", PortCapacity.Multi)] public Vector3 eulerAngles;
        [Port(FluxPortDirection.Output, "Quaternion", PortCapacity.Multi)] public Quaternion quaternion;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            if (target != null)
            {
                if (space == Space.World) {
                    eulerAngles = target.eulerAngles;
                    quaternion = target.rotation;
                } else {
                    eulerAngles = target.localEulerAngles;
                    quaternion = target.localRotation;
                }
            }
        }
    }
}