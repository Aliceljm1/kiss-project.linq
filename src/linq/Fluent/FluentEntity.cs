using System;

namespace Kiss.Linq.Fluent
{
    /// <summary>
    /// Contains Entity Info.
    /// </summary>
    public class FluentEntity
    {
        /// <summary>
        /// Creates a new instance of <see cref="FluentEntity"/>
        /// </summary>
        /// <param name="bucket"></param>
        public FluentEntity(IBucket bucket)
        {
            this.bucket = bucket;
        }

        /// <summary>
        ///  name of the entity, can be overriden by <c>OriginalEntityNameAttribute</c>
        /// </summary>
        public string Name
        {
            get
            {
                return bucket.Name;
            }
        }

        /// <summary>
        /// Gets items to fetch from source.
        /// </summary>
        public int? ItemsToFetch
        {
            get
            {
                return bucket.ItemsToTake;
            }
        }

        /// <summary>
        /// default  0, number of items to skip from start.
        /// </summary>
        public int ItemsToSkipFromStart
        {
            get
            {
                return bucket.ItemsToSkip;
            }
        }

        /// <summary>
        /// list of unique column name.
        /// </summary>
        public string UniqueAttribte
        {
            get
            {
                if (bucket.UniqueItems.Length == 0)
                    return string.Empty;
                return bucket.UniqueItems[0];
            }
        }

        /// <summary>
        /// Defines a fluent implentation for order by query.
        /// </summary>
        public class FluentOrderBy
        {
            /// <summary>
            /// Creates a new instance of <see cref="FluentOrderBy"/>
            /// </summary>
            /// <param name="bucket"></param>
            public FluentOrderBy(IBucket bucket)
            {
                this.bucket = bucket;
            }

            /// <summary>
            /// Callback handler for <see cref="FluentOrderBy"/>
            /// </summary>
            /// <param name="field">field name</param>
            /// <param name="ascending">bool for sort order</param>
            public delegate void Callback(string field, bool ascending);

            /// <summary>
            /// Checks if orderby is used in query and calls action delegate to 
            /// execute user's code and internally marks <value>true</value> for ifUsed field
            /// to be used by <see cref="FluentOrderByItem"/> iterator.
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            public FluentOrderBy IfUsed(Action action)
            {
                ifUsed = bucket.OrderByItems.Count > 0;

                if (ifUsed && action != null)
                    action.DynamicInvoke();
                return this;
            }

            /// <summary>
            /// Iterator for order by items.
            /// </summary>
            public FluentOrderByItem ForEach
            {
                get
                {
                    return new FluentOrderByItem(bucket, ifUsed);
                }
            }

            /// <summary>
            /// Order by iterator.
            /// </summary>
            public class FluentOrderByItem
            {
                /// <summary>
                /// Creates a new instance of <see cref="FluentOrderBy"/>
                /// </summary>
                /// <param name="bucket"></param>
                /// <param name="ifUsed">Defines if a order by is used.</param>
                public FluentOrderByItem(IBucket bucket, bool ifUsed)
                {
                    this.bucket = bucket;
                    this.ifUsed = ifUsed;
                }

                /// <summary>
                /// Does a callback to process the order by used in where clause.
                /// </summary>
                /// <param name="callback"></param>
                public void Process(Callback callback)
                {
                    foreach (Bucket.OrderByInfo info in bucket.OrderByItems)
                    {
                        callback.Invoke(info.FieldName, info.IsAscending);
                    }
                }

                private readonly IBucket bucket;
                private readonly bool ifUsed;
            }

            private bool ifUsed = false;
            private readonly IBucket bucket;
        }

        public FluentOrderBy OrderBy
        {
            get
            {
                return new FluentOrderBy(bucket);
            }
        }

        private readonly IBucket bucket;
    }
}