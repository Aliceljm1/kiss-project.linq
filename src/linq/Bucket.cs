using System.Collections.Generic;
using System.Linq;

namespace Kiss.Linq
{
    /// <summary>
    /// Bucket is stuctured represtion of the orignal query object.
    /// </summary>
    public class Bucket : IBucket
    {
        /// <summary>
        /// Name of the node, either the class name or value of <c>OriginalEntityName</c>, if used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets/Sets <value>true</value> if an where is clause used.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Gets/Sets Items to Take from collection.
        /// </summary>
        public int? ItemsToTake { get; set; }

        /// <summary>
        /// Gets/ Sets items to skip from start.
        /// </summary>
        public int ItemsToSkip { get; set; }

        /// <summary>
        /// Returns property name for which the UniqueIdentifierAttribute is defined.
        /// </summary>
        public string[ ] UniqueItems
        {
            get
            {
                if ( uniqueItemNames == null )
                {
                    var query = from prop in items
                                where prop.Value.Unique
                                select prop.Value.Name;

                    uniqueItemNames = query.ToArray ( );
                }
                return uniqueItemNames;
            }
        }

        /// <summary>
        /// Contains property items for current bucket.
        /// </summary>
        public IDictionary<string, BucketItem> Items
        {
            get
            {
                if ( items == null )
                    items = new Dictionary<string, BucketItem> ( );
                return items;
            }
            internal set
            {
                items = value;
            }
        }

        /// <summary>
        /// Gets the first tree node fro simplied expression tree.
        /// </summary>
        public TreeNode CurrentNode
        {
            get
            {
                if ( node == null )
                    node = new TreeNode ( );
                return node;
            }
            internal set
            {
                node = value;
            }
        }

        /// <summary>
        /// Gets/Sets the current <see cref="CurrentNode"/>
        /// </summary>
        public TreeNode CurrentTreeNode { get; set; }

        /// <summary>
        /// The Filled up with query order by information.
        /// </summary>
        public class OrderByInfo
        {
            private readonly string _field = string.Empty;
            private readonly bool _asc = true;

            public string FieldName
            {
                get
                {
                    return _field;
                }
            }

            public bool IsAscending
            {
                get
                {
                    return _asc;
                }
            }

            internal OrderByInfo ( string field, bool asc )
            {
                _field = field;
                _asc = asc;
            }
        }

        /// <summary>
        /// Holds order by information.
        /// </summary>
        public IList<OrderByInfo> OrderByItems
        {
            get
            {
                if ( orderByItems == null )
                    orderByItems = new List<OrderByInfo> ( );
                return orderByItems;
            }
        }

        /// <summary>
        /// Gets unique identifier properties.
        /// </summary>
        internal string[ ] UniqueProperties
        {
            get
            {
                if ( uniquePropertyNames == null )
                {
                    var query = from prop in items
                                where prop.Value.Unique
                                select prop.Value.ProperyName;

                    uniquePropertyNames = query.ToArray ( );
                }
                return uniquePropertyNames;
            }
        }

        /// <summary>
        /// Clears out any used properties.
        /// </summary>
        protected void Clear ( )
        {
            ItemsToSkip = 0;
            ItemsToTake = null;
            OrderByItems.Clear ( );
            IsDirty = false;
        }

        private string[] uniqueItemNames;
        private string[] uniquePropertyNames;
        private IDictionary<string, BucketItem> items;
        private TreeNode node;
        private IList<OrderByInfo> orderByItems;
    }
}
