using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Core.Data;

namespace FluxFramework.Extensions
{
    [Serializable]
    public class ReactiveDictionary<TKey, TValue> : ReactiveProperty<SerializableDictionary<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, ISerializationCallbackReceiver
    {
        public event Action<TKey, TValue> OnItemAdded;
        public event Action<TKey> OnItemRemoved;
        public event Action<TKey, TValue> OnItemChanged;
        public event Action OnCleared;

        // Store previous state for change detection
        [NonSerialized]
        private Dictionary<TKey, TValue> _previousState = new Dictionary<TKey, TValue>();
        [NonSerialized]
        private bool _isDeserializing = false;

        public ReactiveDictionary() : base(new SerializableDictionary<TKey, TValue>()) 
        {
            InitializePreviousState();
        }
        
        public ReactiveDictionary(IDictionary<TKey, TValue> initialDict) : base(new SerializableDictionary<TKey, TValue>(initialDict)) 
        {
            InitializePreviousState();
        }

        private void InitializePreviousState()
        {
            _previousState.Clear();
            if (Value != null)
            {
                foreach (var kvp in Value)
                {
                    _previousState[kvp.Key] = kvp.Value;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            var oldValue = new SerializableDictionary<TKey, TValue>(Value);
            Value.Add(key, value);
            _previousState[key] = value;
            OnItemAdded?.Invoke(key, value);
            NotifyChange(oldValue, Value);
        }

        public bool Remove(TKey key)
        {
            var oldValue = new SerializableDictionary<TKey, TValue>(Value);
            if (Value.Remove(key))
            {
                _previousState.Remove(key);
                OnItemRemoved?.Invoke(key);
                NotifyChange(oldValue, Value);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (Value.Count == 0) return;
            var oldValue = new SerializableDictionary<TKey, TValue>(Value);
            Value.Clear();
            _previousState.Clear();
            OnCleared?.Invoke();
            NotifyChange(oldValue, Value);
        }

        public TValue this[TKey key]
        {
            get => Value[key];
            set
            {
                var oldValue = new SerializableDictionary<TKey, TValue>(Value);
                Value[key] = value;
                _previousState[key] = value;
                OnItemChanged?.Invoke(key, value);
                NotifyChange(oldValue, Value);
            }
        }

        public int Count => Value.Count;
        public bool ContainsKey(TKey key) => Value.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);
        public ICollection<TKey> Keys => Value.Keys;
        public ICollection<TValue> Values => Value.Values;
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Value.GetEnumerator();
        IEnumerable<KeyValuePair<TKey, TValue>> AsEnumerable() => Value;
        IEnumerator IEnumerable.GetEnumerator() => Value.GetEnumerator();

        /// <summary>
        /// Force serialization of the internal dictionary.
        /// Call this after making changes to ensure Unity serialization is up to date.
        /// </summary>
        public void ForceSerialization()
        {
            Value?.ForceSerialization();
        }

        /// <summary>
        /// Validates the internal state of the dictionary.
        /// </summary>
        public bool ValidateState(out string errorMessage)
        {
            if (Value == null)
            {
                errorMessage = "Internal SerializableDictionary is null";
                return false;
            }
            
            return Value.ValidateState(out errorMessage);
        }

        #region Unity Serialization Callbacks

        public void OnBeforeSerialize()
        {
            // Unity is about to serialize - nothing special needed here
            _isDeserializing = false;
        }

        public void OnAfterDeserialize()
        {
            // Unity just deserialized - check for changes made in Inspector
            _isDeserializing = true;
            
            // Check for changes immediately
            // We'll use a try-catch to handle any timing issues
            try
            {
                CheckForInspectorChanges();
            }
            catch (System.Exception ex)
            {
                // If we can't check now, initialize state for later
                Debug.LogWarning($"Could not check for inspector changes immediately: {ex.Message}");
                InitializePreviousState();
            }
        }

        /// <summary>
        /// Manually check for changes and fire events. 
        /// Call this if you suspect the dictionary was modified externally.
        /// </summary>
        public void ForceChangeDetection()
        {
            CheckForInspectorChanges();
        }

        private void CheckForInspectorChanges()
        {
            if (Value == null || _previousState == null)
            {
                InitializePreviousState();
                return;
            }

            try
            {
                // Create current state snapshot
                var currentState = Value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                // Find additions
                var additions = currentState.Where(kvp => !_previousState.ContainsKey(kvp.Key)).ToList();
                
                // Find removals
                var removals = _previousState.Where(kvp => !currentState.ContainsKey(kvp.Key)).ToList();
                
                // Find changes
                var changes = currentState.Where(kvp => 
                    _previousState.ContainsKey(kvp.Key) && 
                    !EqualityComparer<TValue>.Default.Equals(_previousState[kvp.Key], kvp.Value)).ToList();

                // Fire events for additions
                foreach (var addition in additions)
                {
                    OnItemAdded?.Invoke(addition.Key, addition.Value);
                }

                // Fire events for removals
                foreach (var removal in removals)
                {
                    OnItemRemoved?.Invoke(removal.Key);
                }

                // Fire events for changes
                foreach (var change in changes)
                {
                    OnItemChanged?.Invoke(change.Key, change.Value);
                }

                // Check if dictionary was cleared
                if (_previousState.Count > 0 && currentState.Count == 0)
                {
                    OnCleared?.Invoke();
                }

                // Fire general change notification if anything changed
                if (additions.Any() || removals.Any() || changes.Any())
                {
                    var oldDict = new SerializableDictionary<TKey, TValue>(_previousState);
                    NotifyChange(oldDict, Value);
                }

                // Update previous state
                _previousState = currentState;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error checking for inspector changes in ReactiveDictionary: {ex.Message}");
                InitializePreviousState();
            }
        }

        #endregion

        private void NotifyChange(SerializableDictionary<TKey, TValue> oldValue, SerializableDictionary<TKey, TValue> newValue)
        {
            // Force serialization to ensure Unity sees the changes
            newValue?.ForceSerialization();
            NotifyValueChanged(oldValue, newValue);
        }

        private void NotifyChange()
        {
            NotifyChange(Value, Value);
        }
    }
}