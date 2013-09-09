using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiss.Linq.Fluent
{
    /// <summary>
    /// Fluent iterator entry point.
    /// </summary>
    public class FluentIterator
    {
        /// <summary>
        /// Create a new instance of <see cref="FluentIterator"/> for <see cref="bucket"/>
        /// </summary>
        /// <param name="bucket"></param>
        public FluentIterator ( IBucket bucket )
        {
            this.bucket = bucket;
        }

        /// <summary>
        /// Fluent Item collection implementation.
        /// </summary>
        public class ItemCollection
        {
            /// <summary>
            /// Create a new instance of fluent bucket item.
            /// </summary>
            /// <param name="bucket"></param>
            public ItemCollection ( IBucket bucket )
            {
                this.bucket = bucket;
            }

            /// <summary>
            /// Matches an <see cref="BucketItem"/> for a predicate.
            /// </summary>
            /// <param name="m"></param>
            /// <returns></returns>
            public ItemCollection Match ( Predicate<BucketItem> m )
            {
                this.match = m;
                return this;
            }

            /// <summary>
            /// Raises a callback.
            /// </summary>
            /// <param name="callback"></param>
            public ItemCollection Process ( Callback callback )
            {
                if ( callback == null )
                {
                    throw new LinqException ( Properties.Resource.MustProvideACallback );
                }

                foreach ( string key in bucket.Items.Keys )
                {
                    BucketItem item = bucket.Items[ key ];

                    if ( match != null )
                    {
                        if ( match.Invoke ( item ) )
                        {
                            callback.Invoke ( item );
                        }
                    }
                    else
                    {
                        callback.Invoke ( item );
                    }
                }
                // tear down 
                match = null;

                return this;
            }

            ///<summary>
            ///Fluent method for defining opearator by whihc each bucket item is separted.
            ///</summary>
            ///<param name="action"></param>
            public void SeparationOperation ( Action action )
            {
                this.separtionOperation = action;
            }

            private readonly IBucket bucket;
            private Predicate<BucketItem> match;
            private Action separtionOperation;
            /// <summary>
            /// Callback delegate from <see cref="BucketItem"/>
            /// </summary>
            /// <param name="item"></param>
            public delegate void Callback ( BucketItem item );

        }
        /// <summary>
        /// Gets fluent <see cref="BucketItem"/> collection.
        /// </summary>
        public ItemCollection EachItem
        {
            get
            {
                if ( collecton == null )
                {
                    collecton = new ItemCollection ( bucket );
                }
                return collecton;
            }
        }

        ///<summary>
        /// Gets a <see cref="BucketItem"/> for name
        ///</summary>
        ///<param name="itemName"></param>
        ///<returns></returns>
        public BucketItem Item ( string itemName )
        {
            if ( bucket.Items.ContainsKey ( itemName ) )
            {
                return bucket.Items[ itemName ];
            }
            return new BucketItem ( );
        }

        /// <summary>
        /// Gets <see cref="BucketItem"/> for a property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns><see cref="BucketItem"/></returns>
        public BucketItem Item<T> ( Expression<Func<T, object>> expression )
        {
            MemberInfo memberInfo = expression.GetMemberFromExpression ( );

            if ( memberInfo != null )
            {
                return Item ( memberInfo.Name );
            }
            return null;
        }

        private ItemCollection collecton;
        private readonly IBucket bucket;
    }
}