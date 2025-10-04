using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluxFramework.Core.Data
{
    /// <summary>
    /// A serializable dictionary implementation for Unity.
    /// Implements IDictionary and can be used anywhere a Dictionary would be used,
    /// but with Unity serialization support.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        #region Unity Serialization Callbacks

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            
            foreach (var pair in dictionary)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary.Clear();
            
            for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            {
                if (keys[i] != null && !dictionary.ContainsKey(keys[i]))
                {
                    dictionary.Add(keys[i], values[i]);
                }
            }
        }

        #endregion

        #region IDictionary Implementation

        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.TryGetValue(item.Key, out TValue value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Array is not large enough");

            int index = arrayIndex;
            foreach (var pair in dictionary)
            {
                array[index++] = pair;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Additional Utility Methods

        /// <summary>
        /// Gets all keys as a list (useful for inspector display)
        /// </summary>
        public List<TKey> GetKeysList()
        {
            return new List<TKey>(keys);
        }

        /// <summary>
        /// Gets all values as a list (useful for inspector display)
        /// </summary>
        public List<TValue> GetValuesList()
        {
            return new List<TValue>(values);
        }

        /// <summary>
        /// Force synchronization between internal dictionary and serialized lists
        /// Call this if you modify the dictionary at runtime and want to ensure serialization
        /// </summary>
        public void ForceSerialization()
        {
            OnBeforeSerialize();
        }

        /// <summary>
        /// Force synchronization from serialized lists to internal dictionary
        /// Call this if you modify keys/values lists directly and want to update the dictionary
        /// </summary>
        public void ForceDeserialization()
        {
            OnAfterDeserialize();
        }

        #endregion

        #region Constructors

        public SerializableDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public SerializableDictionary(IDictionary<TKey, TValue> source) : this()
        {
            if (source != null)
            {
                foreach (var pair in source)
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }

        public SerializableDictionary(IEqualityComparer<TKey> comparer) 
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        #endregion

        #region Debug and Inspector Support

        /// <summary>
        /// Returns a string representation of the dictionary for debugging
        /// </summary>
        public override string ToString()
        {
            return $"SerializableDictionary<{typeof(TKey).Name}, {typeof(TValue).Name}> [Count: {Count}]";
        }

        /// <summary>
        /// Validates the internal state and reports any inconsistencies
        /// </summary>
        public bool ValidateState(out string errorMessage)
        {
            errorMessage = "";
            
            if (keys.Count != values.Count)
            {
                errorMessage = $"Keys count ({keys.Count}) doesn't match values count ({values.Count})";
                return false;
            }
            
            if (dictionary.Count != keys.Count)
            {
                errorMessage = $"Dictionary count ({dictionary.Count}) doesn't match serialized count ({keys.Count})";
                return false;
            }
            
            // Check for duplicate keys in serialized data
            var keySet = new HashSet<TKey>();
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] != null && !keySet.Add(keys[i]))
                {
                    errorMessage = $"Duplicate key found: {keys[i]}";
                    return false;
                }
            }
            
            return true;
        }

        #endregion
    }
}