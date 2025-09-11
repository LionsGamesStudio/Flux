using System;

namespace FluxFramework.Attributes
{
    /// <summary>
    /// Marks a property as requiring networking synchronization
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxNetworkAttribute : Attribute
    {
        /// <summary>
        /// Network channel for synchronization
        /// </summary>
        public string Channel { get; set; } = "default";

        /// <summary>
        /// Whether to sync to all clients or just the owner
        /// </summary>
        public bool SyncToAll { get; set; } = true;

        /// <summary>
        /// Update frequency in Hz
        /// </summary>
        public float UpdateRate { get; set; } = 20f;

        /// <summary>
        /// Whether to use compression
        /// </summary>
        public bool Compressed { get; set; } = false;
    }

    /// <summary>
    /// Marks a field as cacheable for performance optimization
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class FluxCacheAttribute : Attribute
    {
        /// <summary>
        /// Cache duration in seconds (0 = infinite)
        /// </summary>
        public float Duration { get; set; } = 0f;

        /// <summary>
        /// Cache key for shared caching
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        /// Whether to invalidate cache on property change
        /// </summary>
        public bool InvalidateOnChange { get; set; } = true;

        /// <summary>
        /// Maximum cache size
        /// </summary>
        public int MaxSize { get; set; } = 100;
    }

    /// <summary>
    /// Marks a method as a command that can be executed remotely
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class FluxCommandAttribute : Attribute
    {
        /// <summary>
        /// Command name for remote execution
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Whether this command requires authentication
        /// </summary>
        public bool RequireAuth { get; set; } = false;

        /// <summary>
        /// Permission level required
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// Whether command can be executed in edit mode
        /// </summary>
        public bool AllowInEditMode { get; set; } = false;

        public FluxCommandAttribute(string commandName = null)
        {
            CommandName = commandName;
        }
    }

    /// <summary>
    /// Marks a property as serializable for save/load operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FluxSerializableAttribute : Attribute
    {
        /// <summary>
        /// Custom serializer type
        /// </summary>
        public Type SerializerType { get; set; }

        /// <summary>
        /// Whether to encrypt the serialized data
        /// </summary>
        public bool Encrypted { get; set; } = false;

        /// <summary>
        /// Version for backward compatibility
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Whether to compress the data
        /// </summary>
        public bool Compressed { get; set; } = false;
    }
}
