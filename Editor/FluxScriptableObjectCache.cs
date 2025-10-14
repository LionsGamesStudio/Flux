using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Editor
{
    /// <summary>
    /// This ScriptableObject is used to cache the GUIDs of all FluxScriptableObjects in the project.
    /// It helps optimize the search process by avoiding repeated asset database queries.
    /// </summary>
    public class FluxScriptableObjectCache : ScriptableObject
    {
        public List<string> fluxScriptableObjectGUIDs = new List<string>();
        public bool isCacheDirty = true; // By default, we consider the cache invalid
    }
}