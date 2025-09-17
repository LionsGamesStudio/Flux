using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;
using FluxFramework.Core;

namespace FluxFramework.VisualScripting.Node
{
    [Serializable]
    [FluxNode("Get Flux Components", Category = "Framework/Components", Description = "Finds all components inheriting from FluxMonoBehaviour on a GameObject and its children.")]
    public class GetFluxComponentsNode : IVolatileNode
    {
        [Tooltip("If true, the search will include inactive GameObjects in the hierarchy.")]
        public bool includeInactive = false;
        
        [Port(FluxPortDirection.Input, "Target", PortCapacity.Single)]
        public GameObject target;

        [Port(FluxPortDirection.Output, "Components", PortCapacity.Multi)]
        public FluxMonoBehaviour[] components;
        
        public void Execute(Execution.FluxGraphExecutor executor, AttributedNodeWrapper wrapper, string triggeredPortName, Dictionary<string, object> dataInputs)
        {
            this.components = Array.Empty<FluxMonoBehaviour>();
            if (target == null) return;

            this.components = target.GetComponentsInChildren<FluxMonoBehaviour>(includeInactive);
        }
    }
}