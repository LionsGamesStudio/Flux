using System;

namespace FluxFramework.Configuration
{
    /// <summary>
    /// Property definition for reactive properties
    /// </summary>
    [Serializable]
    public class PropertyDefinition
    {
        public string key;
        public PropertyType type;
        public string defaultValue;
        public string description;
        public bool isGlobal;
    }
}
