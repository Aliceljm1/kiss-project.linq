using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kiss.Linq
{
    internal class ProjectedQuery<T, S> : ReadOnlyQueryCollection<S>, IQueryProvider, IQueryable<S> where T : IQueryObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedQuery{T,S}"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="query">The query.</param>
        public ProjectedQuery ( Expression expression, Query<T> query )
        {
            this.expression = expression;
            this.query = query;
        }

        #region IEnumerable<TS> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<S> GetEnumerator ( )
        {
            this.ProcessGenericList ( );
            return Items.GetEnumerator ( );
        }


        private void ProcessGenericList ( )
        {
            Items.Clear ( );

            UnaryExpression uExp = QueryExtension.GetUnaryExpressionFromMethodCall ( this.expression );

            if ( uExp.Operand is LambdaExpression )
            {
                var result = query.Select<T, S> ( ( ( Expression<Func<T, S>> ) uExp.Operand ).Compile ( ) );

                Items.AddRange ( result );
            }
        }
        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator ( )
        {
            return ( this as IEnumerable<S> ).GetEnumerator ( );
        }



        #endregion

        #region IQueryable Members

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="T:System.Linq.IQueryable"/> is executed.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.</returns>
        public Type ElementType
        {
            get { return typeof ( S ); }
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable"/>.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.</returns>
        public Expression Expression
        {
            get { return expression; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.</returns>
        public IQueryProvider Provider
        {
            get { return this; }
        }

        #endregion

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement> ( Expression expression )
        {
            return query.CreateQuery<TElement> ( expression );
        }

        public IQueryable CreateQuery ( Expression expression )
        {
            return query.CreateQuery<S> ( expression );
        }

        public TResult Execute<TResult> ( Expression expression )
        {
            return ( TResult ) this.ExecuteNonGeneric<TResult> ( expression );
        }

        public object Execute ( Expression expression )
        {
            return ( S ) this.ExecuteNonGeneric<S> ( expression );
        }

        public object ExecuteNonGeneric<TResult> ( Expression expression )
        {
            ProcessGenericList ( );

            if ( expression is MethodCallExpression )
            {
                MethodCallExpression mCallExp = ( MethodCallExpression ) expression;
                // when first , last or single is called 
                string methodName = mCallExp.Method.Name;

                /* Try for Generics Results */
                Type itemType = typeof ( IQuery<TResult> );

                object obj = QueryExtension.InvokeMethod ( methodName, itemType, this );

                /* Try for Non Generics Result */
                if ( obj == null )
                {
                    itemType = typeof ( IQuery );
                    obj = QueryExtension.InvokeMethod ( methodName, itemType, this );
                }
                return obj;

            }
            return null;
        }


        #endregion

        private readonly Expression expression;
        private readonly Query<T> query;
    }
}

