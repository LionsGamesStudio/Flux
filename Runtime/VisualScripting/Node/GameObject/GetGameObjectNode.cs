using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get GameObject", Category = "GameObject", Description = "Finds a GameObject in the current scene by its name or tag.")]
    public class GetGameObjectNode : INode
    {
        public enum FindBy { Name, Tag }

        [Tooltip("How to find the GameObject.")]
        public FindBy findBy = FindBy.Name;
        [Tooltip("The name or tag to search for.")]
        public string identifier;
        
        [Port(FluxPortDirection.Output, "GameObject", PortCapacity.Multi)]
        public GameObject result;

        public void Execute(AttributedNodeWrapper wrapper)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                Debug.LogWarning("Get GameObject Node: Identifier is empty.", wrapper);
                result = null;
                return;
            }
            
            result = findBy switch
            {
                FindBy.Name => GameObject.Find(identifier),
                FindBy.Tag => GameObject.FindWithTag(identifier),
                _ => null
            };
        }
    }

}