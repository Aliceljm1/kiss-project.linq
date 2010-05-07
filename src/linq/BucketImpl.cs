using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiss.Linq
{
    public class BucketImpl : Bucket
    {
        /// <summary>
        /// Creates a new instance of <see cref="BucketImpl"/> class.
        /// </summary>
        public BucketImpl ( ) { }

        /// <summary>
        /// Creates a new instance of <see cref="BucketImpl"/> class.
        /// </summary>
        /// <param name="targetType"></param>
        public BucketImpl ( Type targetType )
        {
            this.targetType = targetType;
        }

        static internal BucketImpl NewInstance ( Type targetType )
        {
            return new BucketImpl ( targetType );
        }
        /// <summary>
        /// marks if the bucket is already prepared or not.
        /// </summary>
        internal bool IsAlreadyProcessed { get; set; }
        /// <summary>
        /// internal use : to check if the bucket object should be sorted in asc or dsc
        /// </summary>
        internal bool IsAsc { get; set; }

        /// <summary>
        /// Defines the current expression node.
        /// </summary>
        internal ExpressionType CurrentExpessionType { get; set; }
        /// <summary>
        /// number of items queried in <c>Where</c> caluse
        /// </summary>
        internal int ClauseItemCount { get; set; }
        /// <summary>
        /// gets the Level of the clause item
        /// </summary>
        internal int Level { get; set; }

        public BucketImpl Describe ( )
        {
            object[] attr = targetType.GetCustomAttributes ( typeof ( OriginalEntityNameAttribute ), true );
            if ( attr != null && attr.Length > 0 )
            {
                OriginalEntityNameAttribute originalEntityNameAtt = attr[ 0 ] as OriginalEntityNameAttribute;
                if ( originalEntityNameAtt != null ) this.Name = originalEntityNameAtt.EntityName;
            }
            else
            {
                Name = targetType.Name;
            }
            // clear out;
            Clear ( );

            Items = CreateItems ( targetType );

            return this;
        }

        internal Stack<TreeNodeInfo> SyntaxStack
        {
            get
            {
                if ( syntaxStack == null )
                    syntaxStack = new Stack<TreeNodeInfo> ( );

                return syntaxStack;
            }
        }

        internal class TreeNodeInfo
        {
            public int Level
            {
                get;
                set;
            }
            public OperatorType OperatorType { get; set; }
            /// <summary>
            /// identifier
            /// </summary>
            public Guid Id { get; set; }
            public Guid ParentId { get; set; }
        }

        internal RelationType Relation
        {
            get
            {
                RelationType relType = RelationType.Equal;

                switch ( CurrentExpessionType )
                {
                    case ExpressionType.Equal:
                        relType = RelationType.Equal;
                        break;
                    case ExpressionType.GreaterThan:
                        relType = RelationType.GreaterThan;
                        break;
                    case ExpressionType.LessThan:
                        relType = RelationType.LessThan;
                        break;
                    case ExpressionType.NotEqual:
                        relType = RelationType.NotEqual;
                        break;
                    case ExpressionType.LessThanOrEqual:
                        relType = RelationType.LessThanEqual;
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        relType = RelationType.GreaterThanEqual;
                        break;
                }
                return relType;
            }

        }

        /// <summary>
        /// clear outs the data.
        /// </summary>
        protected new void Clear ( )
        {
            base.Clear ( );

            ClauseItemCount = 0;
            CurrentExpessionType = ExpressionType.Equal;
            IsAlreadyProcessed = false;
        }

        public BucketImpl InstanceImpl
        {
            get
            {
                return this;
            }
        }

        private IDictionary<string, BucketItem> CreateItems ( Type targetType )
        {
            PropertyInfo[] infos = targetType.GetProperties ( );

            IDictionary<string, BucketItem> list = new Dictionary<string, BucketItem> ( );

            foreach ( PropertyInfo info in infos )
            {
                string fieldName = string.Empty;

                // assume the property is not unique.
                bool isUnique = false;

                object[] arg = info.GetCustomAttributes ( typeof ( IgnoreAttribute ), false );

                if ( arg.Length == 0 )
                {
                    const bool visible = true;

                    arg = info.GetCustomAttributes ( typeof ( OriginalFieldNameAttribute ), false );

                    if ( arg.Length > 0 )
                    {
                        var fieldNameAttr = arg[ 0 ] as OriginalFieldNameAttribute;

                        if ( fieldNameAttr != null )
                            fieldName = fieldNameAttr.FieldName;
                    }
                    else
                    {
                        fieldName = info.Name;
                    }

                    arg = info.GetCustomAttributes ( typeof ( UniqueIdentifierAttribute ), false );

                    if ( arg.Length > 0 )
                    {
                        isUnique = true;
                    }

                    // only if not already added.
                    if ( !list.ContainsKey ( info.Name ) )
                    {
                        var newItem = new BucketItem ( targetType, fieldName, info.Name, info.PropertyType, null, isUnique, RelationType.Equal, visible ) { };
                        list.Add ( info.Name, newItem );
                    }
                }
            }
            return list;
        }

        private Stack<TreeNodeInfo> syntaxStack;
        private Type targetType;
    }

    internal class BucketImpl<T> : BucketImpl
    {
        public BucketImpl ( ) : base ( typeof ( T ) ) { }

        #region Fluent BucketImpl

        static new internal BucketImpl<T> NewInstance
        {
            get
            {
                return new BucketImpl<T> ( );
            }
        }

        #endregion
    }
}