using System;
using UnityEngine;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Event definition for framework events
    /// </summary>
    [Serializable]
    public class EventDefinition
    {
        public string eventName;
        public string description;
        public bool isGlobal;
        public Color debugColor = Color.white;
    }
}
