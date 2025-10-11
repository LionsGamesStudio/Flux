using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Utils;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// Collection reactive property for lists and arrays
    /// </summary>
    /// <typeparam name="T">Type of collection items</typeparam>
    [Serializable]
    public class ReactiveCollection<T> : ReactiveProperty<List<T>>, IImplicitSyncable
    {
        /// <summary>
        /// Event raised when items are added to the collection
        /// </summary>
        public event Action<IEnumerable<T>> OnItemsAdded;

        /// <summary>
        /// Event raised when items are removed from the collection
        /// </summary>
        public event Action<IEnumerable<T>> OnItemsRemoved;

        /// <summary>
        /// Event raised when the collection is cleared
        /// </summary>
        public event Action OnCleared;

        public ReactiveCollection() : base(new List<T>()) { }

        public ReactiveCollection(IEnumerable<T> initialItems) : base(new List<T>(initialItems)) { }

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <param name="item">Item to add</param>
        public void Add(T item)
        {
            var oldValue = new List<T>(Value); 
            Value.Add(item);
            OnItemsAdded?.Invoke(new[] { item });
            NotifyChange(oldValue, Value);
        }

        /// <summary>
        /// Adds multiple items to the collection
        /// </summary>
        /// <param name="items">Items to add</param>
        public void AddRange(IEnumerable<T> items)
        {
            var itemsArray = items.ToArray();
            if (itemsArray.Length == 0) return;
            var oldValue = new List<T>(Value);
            Value.AddRange(itemsArray);
            OnItemsAdded?.Invoke(itemsArray);
            NotifyChange(oldValue, Value);
        }

        /// <summary>
        /// Removes an item from the collection
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>True if item was removed</returns>
        public bool Remove(T item)
        {
            var oldValue = new List<T>(Value);
            if (Value.Remove(item))
            {
                OnItemsRemoved?.Invoke(new[] { item });
                NotifyChange(oldValue, Value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an item at the specified index
        /// </summary>
        /// <param name="index">Index to remove</param>
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Value.Count)
            {
                var oldValue = new List<T>(Value);
                var item = Value[index];
                Value.RemoveAt(index);
                OnItemsRemoved?.Invoke(new[] { item });
                NotifyChange(oldValue, Value);
            }
        }

        /// <summary>
        /// Clears all items from the collection
        /// </summary>
        public void Clear()
        {
            if (Value.Count == 0) return;
            var oldValue = new List<T>(Value);
            Value.Clear();
            OnCleared?.Invoke();
            NotifyChange(oldValue, Value);
        }

        /// <summary>
        /// Gets the count of items in the collection
        /// </summary>
        public int Count => Value.Count;

        /// <summary>
        /// Gets an item at the specified index
        /// </summary>
        /// <param name="index">Index of the item</param>
        /// <returns>Item at index</returns>
        public T this[int index]
        {
            get => Value[index];
            set
            {
                var oldValue = new List<T>(Value);
                var oldItemInPlace = Value[index];
                Value[index] = value;
                OnItemsRemoved?.Invoke(new[] { oldItemInPlace });
                OnItemsAdded?.Invoke(new[] { value });
                NotifyChange(oldValue, Value);
            }
        }

        /// <summary>
        /// Checks if the collection contains an item
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>True if item exists</returns>
        public bool Contains(T item)
        {
            return Value.Contains(item);
        }

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">Item to find</param>
        /// <returns>Index of item or -1 if not found</returns>
        public int IndexOf(T item)
        {
            return Value.IndexOf(item);
        }

        private void NotifyChange(List<T> oldValue, List<T> newValue)
        {
            NotifyValueChanged(oldValue, newValue);
        }

        #region IImplicitSyncable Implementation

        /// <summary>
        /// Sets up the subscriptions to keep the local List<T> field in sync.
        /// </summary>
        public void SetupImplicitSync(MonoBehaviour owner, object localFieldInstance)
        {
            if (localFieldInstance is not IList localList)
            {
                Debug.LogError($"[FluxFramework] IImplicitSyncable setup failed for ReactiveCollection. The provided local field is not an IList.", owner);
                return;
            }

            var subManager = owner.gameObject.GetComponent<ComponentSubscriptionManager>() ?? owner.gameObject.AddComponent<ComponentSubscriptionManager>();

            // The logic is now strongly typed and much cleaner!
            Action<IEnumerable<T>> addHandler = items => { foreach (var item in items) localList.Add(item); };
            this.OnItemsAdded += addHandler;

            Action<IEnumerable<T>> removeHandler = items => { foreach (var item in items) localList.Remove(item); };
            this.OnItemsRemoved += removeHandler;

            Action clearHandler = localList.Clear;
            this.OnCleared += clearHandler;

            // Ensure we clean up the subscriptions when the component is destroyed
            subManager.Add(new ActionDisposable(() =>
            {
                this.OnItemsAdded -= addHandler;
                this.OnItemsRemoved -= removeHandler;
                this.OnCleared -= clearHandler;
            }));
        }

        #endregion
    }
}
