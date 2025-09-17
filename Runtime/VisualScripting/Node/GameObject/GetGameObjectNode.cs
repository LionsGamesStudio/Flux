using System;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using System.Collections.Generic;

namespace FluxFramework.VisualScripting.Node
{ 
    [Serializable]
    [FluxNode("Get GameObject", Category = "GameObject", Description = "Finds or references a GameObject in the scene.")]
    public class GetGameObjectNode : IVolatileNode
    {
        // --- Configuration Fields ---
        public enum FindMode { ByName, ByTag, ByComponentType, DirectReference }
        
        [Tooltip("How to find the GameObject.")]
        public FindMode Mode = FindMode.ByName;
        
        [Tooltip("A direct reference to a GameObject in the scene or project.")]
        public GameObject Reference;
        
        [Tooltip("The component type to search for.")]
        public Component ComponentTypeReference;
        
        // --- Input & Output Ports ---
        [Port(FluxPortDirection.Input, "Identifier", portType: FluxPortType.Data, PortCapacity.Single)]
        public string Identifier;

        [Port(FluxPortDirection.Output, "GameObject", portType: FluxPortType.Data, PortCapacity.Multi)]
        public GameObject Result;

        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            Result = null;
            // The 'Identifier' field is now an input port, so its value is automatically
            // populated by the executor from a connection OR it keeps its Inspector value.
            
            switch (Mode)
            {
                case FindMode.ByName:
                    if (!string.IsNullOrEmpty(Identifier)) Result = GameObject.Find(Identifier);
                    break;
                case FindMode.ByTag:
                    if (!string.IsNullOrEmpty(Identifier)) Result = GameObject.FindWithTag(Identifier);
                    break;
                case FindMode.DirectReference:
                    Result = Reference;
                    break;
                case FindMode.ByComponentType:
                    if (ComponentTypeReference != null)
                    {
                        var typeToFind = ComponentTypeReference.GetType();
                        var foundComponent = UnityEngine.Object.FindObjectOfType(typeToFind) as Component; 
                        if (foundComponent != null)
                        {
                            Result = foundComponent.gameObject;
                        }
                    }
                    break;
            }
        }
    }
}