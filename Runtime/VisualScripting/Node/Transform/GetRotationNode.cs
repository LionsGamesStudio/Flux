using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get Rotation", Category = "Transform", Description = "Gets the world or local rotation of a Transform.")]
    public class GetRotationNode : INode
    {
        public Space space = Space.World;
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)] public Transform target;
        [Port(FluxPortDirection.Output, "Euler Angles", PortCapacity.Multi)] public Vector3 eulerAngles;
        [Port(FluxPortDirection.Output, "Quaternion", PortCapacity.Multi)] public Quaternion quaternion;
        
        public void Execute()
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