using System;
using System.Collections.Generic;
using System.Linq;
using FluxFramework.Core;

namespace FluxFramework.Extensions
{
    /// <summary>
    /// Collection reactive property for lists and arrays
    /// </summary>
    /// <typeparam name="T">Type of collection items</typeparam>
    public class ReactiveCollection<T> : ReactiveProperty<List<T>>
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
            Value.Add(item);
            OnItemsAdded?.Invoke(new[] { item });
            NotifyCollectionChanged();
        }

        /// <summary>
        /// Adds multiple items to the collection
        /// </summary>
        /// <param name="items">Items to add</param>
        public void AddRange(IEnumerable<T> items)
        {
            var itemsArray = items.ToArray();
            Value.AddRange(itemsArray);
            OnItemsAdded?.Invoke(itemsArray);
            NotifyCollectionChanged();
        }

        /// <summary>
        /// Removes an item from the collection
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <returns>True if item was removed</returns>
        public bool Remove(T item)
        {
            if (Value.Remove(item))
            {
                OnItemsRemoved?.Invoke(new[] { item });
                NotifyCollectionChanged();
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
                var item = Value[index];
                Value.RemoveAt(index);
                OnItemsRemoved?.Invoke(new[] { item });
                NotifyCollectionChanged();
            }
        }

        /// <summary>
        /// Clears all items from the collection
        /// </summary>
        public void Clear()
        {
            Value.Clear();
            OnCleared?.Invoke();
            NotifyCollectionChanged();
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
                var oldItem = Value[index];
                Value[index] = value;
                OnItemsRemoved?.Invoke(new[] { oldItem });
                OnItemsAdded?.Invoke(new[] { value });
                NotifyCollectionChanged();
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

        private void NotifyCollectionChanged()
        {
            // Trigger the property change notification
            var currentValue = Value;
            Value = new List<T>(currentValue);
        }
    }
}
