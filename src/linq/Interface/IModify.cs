using System.Collections.Generic;

namespace Kiss.Linq
{
    /// <summary>
    /// Generic inteface for modifying collecion.
    /// </summary>
    public interface IModify
    {
        /// <summary>
        /// Clears out items from collection.
        /// </summary>
        void Clear();
        /// <summary>
        /// Sorts the collection, using the orderby clause used in query.
        /// </summary>
        void Sort();
    }

    /// <summary>
    /// Non generic interface for modifying colleciton items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IModify<T> : IQuery<T>, IQuery, IModify
    {
        /// <summary>
        /// Marks an item to be removed.
        /// </summary>
        /// <param name="value">query object.</param>
        void Remove(T value);

        void Remove(IEnumerable<T> items);
        /// <summary>
        /// Addes a range of items to the collection.
        /// </summary>
        /// <param name="items"></param>
        void AddRange(IEnumerable<T> items);
        /// <summary>
        /// Adds items to the main collection and does a sort operation if any orderby is used in query.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="inMemorySort"></param>
        void AddRange(IEnumerable<T> items, bool inMemorySort);
        /// <summary>
        /// Adds a new item to the collection
        /// </summary>
        /// <param name="item"></param>
        void Add(T item);

        void Add(T item, bool isNew);
    }
}
