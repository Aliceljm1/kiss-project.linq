using System;

namespace Kiss.Linq.Fluent
{
    /// <summary>
    /// Fluent implementation for the simplified expression tree.
    /// </summary>
    public class FluentExpressionTree
    {
        /// <summary>
        /// Creates a new instance of the <see cref="FluentExpressionTree"/>
        /// </summary>
        /// <param name="node"></param>
        public FluentExpressionTree ( TreeNode node )
        {
            this.node = node;
        }
        /// <summary>
        /// Gets a <see cref="TreeNode"/>
        /// </summary>
        public TreeNode Node
        {
            get
            {
                return node;
            }
        }
        /// <summary>
        /// Describes the contain for which the express tree will be evaluated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public FluentExpressionTree<T> DescribeContainerAs<T> ( T reference )
        {
            return new FluentExpressionTree<T> ( this.Node, reference );
        }

        private readonly TreeNode node;
    }
    /// <summary>
    /// Non generic implemenation of the fluent expression tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FluentExpressionTree<T> : FluentExpressionTree
    {
        /// <summary>
        /// Creates a new Settings of the <see cref="FluentExpressionTree"/>
        /// </summary>
        /// <param name="node"></param>
        public FluentExpressionTree ( TreeNode node )
            : base ( node )
        {
        }
        /// <summary>
        /// Creates a new Settings of the <see cref="FluentExpressionTree"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="reference"><typeparamref name="T"/></param>
        public FluentExpressionTree ( TreeNode node, T reference )
            : base ( node )
        {
            this.reference = reference;
        }

        /// <summary>
        /// Invoked for starting <see cref="TreeNode"/>
        /// </summary>
        /// <param name="beginHandler"></param>
        /// <returns></returns>
        public FluentExpressionTree<T> Begin ( BeginHandler beginHandler )
        {
            begin = beginHandler;
            return this;
        }

        /// <summary>
        /// Invoked for closing the <see cref="TreeNode"/>
        /// </summary>
        /// <param name="endHandler"></param>
        /// <returns></returns>
        public FluentExpressionTree<T> End ( EndHandler endHandler )
        {
            end = endHandler;
            return this;
        }
        /// <summary>
        /// Invoked for root <see cref="TreeNode"/>
        /// </summary>
        /// <param name="rootHandler"></param>
        /// <returns></returns>
        public FluentExpressionTree<T> Root ( RootHandler rootHandler )
        {
            root = rootHandler;
            return this;
        }
        /// <summary>
        /// Invoked foreach Leaf
        /// </summary>
        /// <param name="itemHandler"></param>
        /// <returns></returns>
        public FluentExpressionTree<T> EachLeaf ( ItemHandler itemHandler )
        {
            this.itemHandler = itemHandler;
            return this;
        }
        /// <summary>
        /// Begin <see cref="TreeNode"/> handler.
        /// </summary>
        /// <param name="sender"></param>
        public delegate void BeginHandler ( T sender );
        /// <summary>
        /// Closing <see cref="TreeNode"/> handler.
        /// </summary>
        /// <param name="sender"></param>
        public delegate void EndHandler ( T sender );
        /// <summary>
        /// Root <see cref="TreeNode"/>  handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="operatorType"></param>
        public delegate void RootHandler ( T sender, OperatorType operatorType );
        /// <summary>
        /// Leaf node handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="item"></param>
        public delegate void ItemHandler ( T sender, BucketItem item );

        private BeginHandler begin;
        private EndHandler end;
        private RootHandler root;
        private ItemHandler itemHandler;


        private void BuildNode ( TreeNode tNode )
        {
            if ( begin != null )
                begin ( reference );

            if ( tNode.Left != null )
            {
                BuildItem ( tNode.Left );
            }

            if ( tNode.Root != OperatorType.NONE )
            {
                if ( root != null )
                    root ( reference, tNode.Root );
            }

            if ( tNode.Right != null )
            {
                BuildItem ( tNode.Right );
            }

            if ( end != null )
                end ( reference );
        }

        private void BuildItem ( TreeNode.Node leaf )
        {
            if ( leaf.Value is TreeNode )
            {
                BuildNode ( ( TreeNode ) leaf.Value );
            }
            else if ( leaf.Value is BucketItem )
            {
                BucketItem item = ( BucketItem ) leaf.Value;

                if ( itemHandler != null )
                {
                    itemHandler ( reference, item );
                }
            }
        }
        /// <summary>
        /// Builds the logical tree for the expression.
        /// </summary>
        public void Execute ( )
        {
            if ( Node == null )
                throw new LinqException ( Properties.Resource.MustDefineAContainer );

            BuildNode ( Node );
        }

        private readonly T reference;
    }
}