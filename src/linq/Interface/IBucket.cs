﻿
using System.Collections.Generic;

namespace Kiss.Linq
{
    /// <summary>
    /// Interface defining Bucket object and its accesible proeprties.
    /// </summary>
    public interface IBucket
    {
        /// <summary>
        /// Gets the name of the <see cref="Bucket"/> object, either the class name or value of <c>OriginalEntityName</c>, if used.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets/Sets <value>true</value> if an where is clause used.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Gets/Sets Items to Take from collection.
        /// </summary>
        int? ItemsToTake { get; }

        /// <summary>
        /// Gets/ Sets items to skip from start.
        /// </summary>
        int ItemsToSkip { get; }

        /// <summary>
        /// Gets <see cref="BucketItem"/> for property.
        /// </summary>
        IDictionary<string, BucketItem> Items { get; }

        /// <summary>
        /// Gets the order by information set in query.
        /// </summary>
        IList<Bucket.OrderByInfo> OrderByItems { get; }

        /// <summary>
        /// Returns property name for which the UniqueIdentifierAttribute is defined.
        /// </summary>
        string[ ] UniqueItems { get; }

        /// <summary>
        /// Gets the tree node represting itself and its child nodes.
        /// </summary>
        TreeNode CurrentNode { get; }
    }
}
