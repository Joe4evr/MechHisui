using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace SharedExtensions.Collections
{
    internal delegate Task NotifyCollectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e);

#nullable disable warnings
    internal sealed class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        ///     Initializes a new instance of AsyncObservableCollection
        ///     that is empty and has default initial capacity.
        /// </summary>
        public AsyncObservableCollection()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the AsyncObservableCollection class
        ///     that contains elements copied from the specified list
        /// </summary>
        /// <param name="list">
        ///     The list whose elements are copied to the new list.
        /// </param>
        /// <remarks>
        ///     The elements are copied onto the AsyncObservableCollection in the
        ///     same order they are read by the enumerator of the list.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="list"/> is a null reference
        /// </exception>
        public AsyncObservableCollection(List<T> list)
            : base((list != null) ? new List<T>(list.Count) : list)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the AsyncObservableCollection class that contains
        ///     elements copied from the specified collection and has sufficient capacity
        ///     to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">
        ///     The collection whose elements are copied to the new list.
        /// </param>
        /// <remarks>
        ///     The elements are copied onto the AsyncObservableCollection in the
        ///     same order they are read by the enumerator of the collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="collection"/> is a null reference
        /// </exception>
        public AsyncObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        ///     Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        ///     see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        public event NotifyCollectionChangedAsync CollectionChangedAsync;

        protected override void ClearItems()
        {
            base.ClearItems();
            OnCollectionReset().GetAwaiter().GetResult();
        }

        protected override void RemoveItem(int index)
        {
            var removedItem = this[index];
            base.RemoveItem(index);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index).GetAwaiter().GetResult();
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index).GetAwaiter().GetResult();
        }

        protected override void SetItem(int index, T item)
        {
            var originalItem = this[index];
            base.SetItem(index, item);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item, index).GetAwaiter().GetResult();
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            var removedItem = this[oldIndex];
            base.MoveItem(oldIndex, newIndex);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex).GetAwaiter().GetResult();
        }

        private Task OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
            => CollectionChangedAsync?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));

        private Task OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
            => CollectionChangedAsync?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));

        private Task OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
            => CollectionChangedAsync?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));

        private Task OnCollectionReset()
            => CollectionChangedAsync?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
