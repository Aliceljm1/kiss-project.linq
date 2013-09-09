using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiss.Linq
{
    /// <summary>
    /// Contains query objects.
    /// </summary>
    [Serializable]
    public class QueryCollection<T> : IModify<T> where T : IQueryObject, new()
    {
        #region IItemList<Item> Members

        /// <summary>
        /// Gets/Sets a query object implementation for an index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public QueryObject<T> this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
            }
        }
        /// <summary>
        /// returnts true if the sequece contains any element.
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return list.Count > 0;
        }
        /// <summary>
        /// returns only element of the sequece , throws exception if there is no element in 
        /// the sequece
        /// </summary>
        /// <returns></returns>
        public T Single()
        {
            try
            {
                if (list.Count == 0)
                {
                    throw new LinqException(Properties.Resource.EmptyCollection);
                }

                return list.Single().ReferringObject;
            }
            catch
            {
                throw new LinqException(Properties.Resource.MultipleElementInColleciton);
            }
        }

        /// <summary>
        /// Returns a single item or default value if empty.
        /// </summary>
        /// <returns></returns>
        public T SingleOrDefault()
        {
            if (list.Count == 1)
                return list.Single().ReferringObject;
            if (list.Count > 1)
                throw new LinqException(Properties.Resource.MultipleElementInColleciton);
            return default(T);
        }
        /// <summary>
        /// returns the count of the sequence
        /// </summary>
        /// <returns></returns>
        public object Count()
        {
            return list.Count;
        }
        /// <summary>
        /// returns the first item of the sequence
        /// </summary>
        /// <returns></returns>
        public T First()
        {
            return list.First().ReferringObject;
        }

        /// <summary>
        /// Returns first item or default value if empty.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            if (list.Count > 0)
                return list.First().ReferringObject;
            return default(T);
        }
        /// <summary>
        /// returns the last item of the sequence.
        /// </summary>
        /// <returns></returns>
        public T Last()
        {
            return list.Last().ReferringObject;
        }
        /// <summary>
        /// Returns the last item or a default value.
        /// </summary>
        /// <returns></returns>
        public T LastOrDefault()
        {
            if (list.Count > 0)
                return list.Last().ReferringObject;
            return default(T);
        }
        /// <summary>
        /// Marks a item to be removed from the colleciton.
        /// </summary>
        /// <param name="value"></param>
        public void Remove(T value)
        {
            foreach (QueryObject<T> item in list)
            {
                if (QueryExtension.IsEqual(item.ReferringObject, value))
                {
                    (item as IQueryObjectImpl).IsDeleted = true;
                }
            }
        }
        public void Remove(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Remove(item);
            }
        }

        /// <summary>
        /// Clears out the collection.
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item, bool isNew)
        {
            var queryObject = new QueryObject<T>(item, isNew) { };

            if (!queryObject.IsNewlyAdded)
            {
                (queryObject as IVersionItem).Commit();
            }
            list.Add(queryObject);
        }

        public void Add(T item)
        {
            var queryObject = new QueryObject<T>(item) { };

            if (!queryObject.IsNewlyAdded)
            {
                (queryObject as IVersionItem).Commit();
            }
            list.Add(queryObject);
        }

        /// <summary>
        /// Adds a range of items to the collection.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                // add the ref object.
                Add(item);
            }
        }
        /// <summary>
        /// Adds items to the main collection and does a sort operation if any orderby is used in query.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="inMemorySort"></param>
        public virtual void AddRange(IEnumerable<T> items, bool inMemorySort)
        {
            //impletmented by child class.
        }

        /// <summary>
        /// Sorts the collection, using the orderby clause used in query.
        /// </summary>
        public virtual void Sort()
        {

        }

        #endregion

        /// <summary>
        /// Gets a list of query item.
        /// </summary>
        public List<T> Items
        {
            get
            {
                return list.Select(item => item.ReferringObject).ToList();
            }
        }
        /// <summary>
        /// Gets a list of query object implementation.
        /// </summary>
        internal List<QueryObject<T>> Objects
        {
            get
            {
                return list;
            }
        }
        /// <summary>
        /// Sorts the collection with the provided comparer.
        /// </summary>
        /// <param name="comparer"></param>
        public void Sort(IComparer<QueryObject<T>> comparer)
        {
            list.Sort(comparer);
        }

        private readonly List<QueryObject<T>> list = new List<QueryObject<T>>();
    }
}
