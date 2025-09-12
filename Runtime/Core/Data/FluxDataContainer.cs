using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using FluxFramework.Attributes;

namespace FluxFramework.Core
{
    /// <summary>
    /// A generic data container for managing game data as a ScriptableObject.
    /// It automatically handles reactive property registration and can be extended for features
    /// like versioning, serialization, and logging.
    /// Validation is now handled automatically at the ReactiveProperty level.
    /// </summary>
    public abstract class FluxDataContainer : FluxScriptableObject
    {
        [Header("Data Container Configuration")]
        [Tooltip("If enabled, metadata about the container's version can be used for data migration.")]
        [SerializeField] private bool enableVersioning = true;
        
        [Tooltip("If enabled, any change to a reactive property in this container will be logged to the console.")]
        [SerializeField] private bool logChanges = false;
        
        [Header("Metadata")]
        [SerializeField] private string containerVersion = "1.0.0";
        [SerializeField] private string description = "";

        /// <summary>
        /// Event fired when data is successfully loaded into this container.
        /// </summary>
        public System.Action<FluxDataContainer> OnDataLoaded;
        
        /// <summary>
        /// Event fired when data from this container is saved.
        /// </summary>
        public System.Action<FluxDataContainer> OnDataSaved;

        private readonly Dictionary<string, object> _previousValues = new Dictionary<string, object>();

        /// <summary>
        /// This sealed method is called by the base FluxScriptableObject after its properties are initialized.
        /// It sets up additional features like change logging.
        /// </summary>
        protected sealed override void OnReactivePropertiesInitialized()
        {
            base.OnReactivePropertiesInitialized();
            
            if (logChanges)
            {
                SetupChangeLogging();
            }
            
            StoreCurrentValues();
            
            OnDataContainerInitialized();
        }
        
        /// <summary>
        /// This method can be overridden by child classes for custom initialization logic
        /// that should run after all reactive properties are set up.
        /// </summary>
        protected virtual void OnDataContainerInitialized()
        {
            // Child classes can override this.
        }

        /// <summary>
        /// Subscribes to all reactive properties in this container to log their changes to the console.
        /// </summary>
        private void SetupChangeLogging()
        {
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    var propertyKey = reactiveAttr.Key;
                    var property = Flux.Manager.GetProperty(propertyKey);
                    if (property != null)
                    {
                        property.Subscribe(newValue => LogChange(field.Name, _previousValues.GetValueOrDefault(field.Name), newValue));
                    }
                }
            }
        }

        /// <summary>
        /// Stores the current values of all reactive properties to track changes.
        /// </summary>
        private void StoreCurrentValues()
        {
            _previousValues.Clear();
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    _previousValues[field.Name] = field.GetValue(this);
                }
            }
        }

        /// <summary>
        /// Logs a value change to the Unity console and updates the tracked previous value.
        /// </summary>
        private void LogChange(string fieldName, object oldValue, object newValue)
        {
            Debug.Log($"[{GetType().Name}] Property Changed: '{fieldName}' | {oldValue} → {newValue}", this);
            _previousValues[fieldName] = newValue;
        }

        public virtual bool ValidateData(out List<string> errorMessages)
        {
            errorMessages = new List<string>();
            // Base implementation always passes. Child classes can override for complex rules.
            return true;
        }
        
        /// <summary>
        /// Serializes the data container to a JSON string.
        /// </summary>
        public virtual string SerializeToJson()
        {
            try
            {
                return JsonUtility.ToJson(this, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to serialize {GetType().Name}: {e.Message}", this);
                return "";
            }
        }

        /// <summary>
        /// Loads data into this container from a JSON string.
        /// </summary>
        public virtual void LoadFromJson(string json)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(json, this);
                
                // Re-register reactive properties after loading to sync with the framework.
                InitializeReactiveProperties();
                
                OnDataLoaded?.Invoke(this);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load {GetType().Name} from JSON: {e.Message}", this);
            }
        }

        /// <summary>
        /// Creates a snapshot of the current data for versioning or backup.
        /// </summary>
        public virtual DataSnapshot CreateSnapshot()
        {
            return new DataSnapshot
            {
                containerType = GetType().Name,
                version = containerVersion,
                timestamp = System.DateTime.Now.ToBinary(),
                data = SerializeToJson()
            };
        }

        /// <summary>
        /// Restores the container's data from a snapshot.
        /// </summary>
        public virtual void RestoreFromSnapshot(DataSnapshot snapshot)
        {
            if (snapshot.containerType != GetType().Name)
            {
                Debug.LogWarning($"Snapshot type mismatch: expected {GetType().Name}, got {snapshot.containerType}", this);
                return;
            }

            LoadFromJson(snapshot.data);
        }

        /// <summary>
        /// Gets a summary of all reactive properties and their current values.
        /// </summary>
        [ContextMenu("Get Data Summary")]
        public virtual Dictionary<string, object> GetDataSummary()
        {
            var summary = new Dictionary<string, object>();
            var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var reactiveAttr = field.GetCustomAttribute<ReactivePropertyAttribute>();
                if (reactiveAttr != null)
                {
                    summary[field.Name] = field.GetValue(this);
                }
            }
            
            return summary;
        }

        /// <summary>
        /// Data snapshot structure for versioning and backup.
        /// </summary>
        [System.Serializable]
        public struct DataSnapshot
        {
            public string containerType;
            public string version;
            public long timestamp;
            public string data;
            
            public System.DateTime GetTimestamp()
            {
                return System.DateTime.FromBinary(timestamp);
            }
        }
    }
}