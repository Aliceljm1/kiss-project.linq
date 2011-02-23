using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiss.Linq
{
    ///<summary>
    /// Entry class for LINQ provider. Containter of the virtual methods that will be invoked on select, intsert, update, remove or get calls.
    ///</summary>
    public class Query<T> : ExpressionVisitor, IModify<T>, IOrderedQueryable<T>, IDisposable, IQueryProvider where T : IQueryObject, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="Query{T}"/> class.
        /// </summary>
        public Query()
        {
            this.collection = new QueryCollection<T>();
        }

        /// <summary>
        /// Gets the collection item for an index
        /// </summary>
        /// <param name="index">index</param>
        /// <returns><typeparamref name="T"/></returns>
        public T this[int index]
        {
            get
            {
                return ((QueryCollection<T>)this.collection).Items[index];
            }
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (this as IQueryable).Provider.Execute<IList<T>>(currentExpression).GetEnumerator();
        }

        #endregion

        #region IQueryable Members
        /// <summary>
        /// Gets element type for the expression.
        /// </summary>
        public Type ElementType
        {
            get { return typeof(T); }
        }
        /// <summary>
        /// Gets the expression tree.
        /// </summary>
        public Expression Expression
        {
            get
            {
                return Expression.Constant(this);
            }
        }
        /// <summary>
        /// Gets a query provider the LINQ query.
        /// </summary>
        public IQueryProvider Provider
        {
            get
            {
                return this;
            }
        }

        #endregion

        #region IEnumerable<Item> Members

        /// <summary>
        /// Executes the query and gets a iterator for it.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return (this as IQueryable).Provider.Execute<IList<T>>(currentExpression).GetEnumerator();
        }

        #endregion

        #region IQueryProvider Members

        /// <summary>
        /// Creates the query for type and current expression.
        /// </summary>
        /// <typeparam name="TS">currenty type passed by frameowrk</typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IQueryable<TS> CreateQuery<TS>(Expression expression)
        {
            // make sure there are no previous items left in the collection.
            if ((int)this.Count() > 0)
                this.Clear();

            this.currentExpression = expression;
            MethodCallExpression curentMethodcall = currentExpression as MethodCallExpression;

            if (curentMethodcall != null)
            {
                if (curentMethodcall.Method.Name == CallType.Join)
                {
                    throw new LinqException(Properties.Resource.DirectJoinNotSupported);
                }

                // Create a new bucket when Query<T>.Execute is called or it is empty for current type.
                if ((!Buckets.ContainsKey(typeof(T).FullName)) || Buckets.Current.IsAlreadyProcessed)
                {
                    Buckets.Current = BucketImpl<T>.NewInstance.Describe();
                }

                Buckets.Current.IsAsc = curentMethodcall.Method.Name == CallType.Orderbydesc ? false : true;

                if (curentMethodcall.Arguments.Count == 2)
                {
                    VisitExpression<TS>(curentMethodcall.Arguments[curentMethodcall.Arguments.Count - 1], 0, null);
                }
            }

            if (typeof(T) != typeof(TS))
            {
                projectedQuery = new ProjectedQuery<T, TS>(expression, this);

                return (IQueryable<TS>)projectedQuery;
            }

            return (IQueryable<TS>)((ConstantExpression)curentMethodcall.Arguments[0]).Value;
        }
        /// <summary>
        /// Creates the query for current expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>ref to IQueryable instance</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            return (this as IQueryProvider).CreateQuery<T>(expression);
        }

        /// <summary>
        /// Executes the query for current type and expression
        /// </summary>
        /// <typeparam name="TResult">Current type</typeparam>
        /// <param name="expression"></param>
        /// <returns>typed result</returns>
        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)(this as IQueryProvider).Execute(expression);
        }

        /// <summary>
        /// Executes the query for current expression.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns>object/collection</returns>
        public object Execute(Expression expression)
        {
            if (expression == null)
            {
                // do a generic select;
                expression = (this as IQueryable<T>).Select(x => x).Expression;
            }

            ProcessItem(Buckets.Current);

            if (expression is MethodCallExpression)
            {
                MethodCallExpression mCallExp = (MethodCallExpression)expression;
                // when first , last or single is called 
                string methodName = mCallExp.Method.Name;

                Type itemGenericType = typeof(IQuery<T>);
                Type itemNonGenericType = typeof(IQuery);
                if (mCallExp.Method.ReturnType == typeof(T))
                {
                    return QueryExtension.InvokeMethod(methodName, itemGenericType, this);
                }

                /* Try for Non Generics Result */
                Object obj = QueryExtension.InvokeMethod(methodName, itemNonGenericType, this);
                if (obj != null)
                    return obj;
            }

            return ((QueryCollection<T>)this.collection).Items;
        }

        #endregion


        #region Implementation of IQuery<T>

        /// <summary>
        /// Returns a single item from the collection.
        /// </summary>
        /// <returns></returns>
        public T Single()
        {
            return this.collection.Single();
        }

        /// <summary>
        /// Returns a single item or default value if empty.
        /// </summary>
        /// <returns></returns>
        public T SingleOrDefault()
        {
            return this.collection.SingleOrDefault();
        }

        /// <summary>
        /// Returns the first item from the collection.
        /// </summary>
        /// <returns></returns>
        public T First()
        {
            return this.collection.First();
        }

        /// <summary>
        /// Returns first item or default value if empty.
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            return this.collection.FirstOrDefault();
        }

        /// <summary>
        /// Returns the last item from the collection.
        /// </summary>
        /// <returns></returns>
        public T Last()
        {
            return this.collection.Last();
        }

        /// <summary>
        /// Returns last item or default value if empty.
        /// </summary>
        /// <returns></returns>
        public T LastOrDefault()
        {
            return this.collection.LastOrDefault();
        }

        #endregion

        #region Implementation of IQuery

        /// <summary>
        /// Return true if there is any item in collection.
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            return this.collection.Any();
        }

        /// <summary>
        /// Returns the count of items in the collection.
        /// </summary>
        /// <returns></returns>
        public object Count()
        {
            return this.collection.Count();
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            // clean up expression object from memory.
            if (this.currentExpression != null)
            {
                currentExpression = null;
            }
            Buckets.Clear();
        }

        #endregion

        /// <summary>
        /// Clears out items from collection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// internally tries to sort , if the query contains orderby statement.
        /// </summary>
        public void Sort()
        {
            if (Buckets.Current.OrderByItems != null)
            {
                foreach (var orderByInfo in Buckets.Current.OrderByItems)
                {
                    ((QueryCollection<T>)this.collection).Sort(new QueryItemComparer<QueryObject<T>>(orderByInfo.FieldName, orderByInfo.IsAscending));
                }
            }
        }

        /// <summary>
        /// Marks an item to be removed.
        /// </summary>
        /// <param name="value">query object.</param>
        public void Remove(T value)
        {
            this.collection.Remove(value);
        }

        public void Remove(IEnumerable<T> items)
        {
            this.collection.Remove(items);
        }

        /// <summary>
        /// Addes a range of items to the collection.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items)
        {
            this.collection.AddRange(items);
        }

        /// <summary>
        /// Adds list of items to the collection , optionally calls in memory sort. Used in Query<typeparamref name="T"/>.SelectItem
        /// </summary>
        /// <param name="items">collection</param>
        /// <param name="inMemorySort">true/false</param>
        public void AddRange(IEnumerable<T> items, bool inMemorySort)
        {
            this.collection.AddRange(items);

            if (inMemorySort)
            {
                this.collection.Sort();
            }
        }

        /// <summary>
        /// Adds a new item to the collection
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            this.collection.Add(item);
        }

        public void Add(T item, bool isNew)
        {
            this.collection.Add(item, isNew);
        }

        #region Tobe overriden methods
        /// <summary>
        /// Invoked after SubmitChanges(), if there is new item in the colleciton.
        /// </summary>
        protected virtual bool AddItem(IBucket bucket)
        {
            // do nothing.
            return false;
        }
        /// <summary>
        /// Invoked after SubmitChanges(), if there are delted items in the collection.
        /// </summary>
        protected virtual bool RemoveItem(IBucket bucket)
        {
            // do nothing.
            return false;
        }
        /// <summary>
        /// Invoked after SubmitChanges(), if any of the object value is altered.
        /// </summary>
        protected virtual bool UpdateItem(IBucket bucket)
        {
            // do nothing.
            return false;
        }
        /// <summary>
        /// gets the single item for unique properties.
        /// </summary>
        /// <returns></returns>
        protected virtual T GetItem(IBucket bucket)
        {
            return defaultItem;
        }
        /// <summary>
        /// Called by the extender for select queries.
        /// </summary>
        /// <param name="bucket">bucekt interface.</param>
        /// <param name="items"></param>
        protected virtual void SelectItem(IBucket bucket, IModify<T> items)
        {
            // does nothing.
        }

        #endregion

        ///<summary>
        /// When called, it invokes the appropiate Query<typeparamref name="T"/> method to finalize the collection changes.
        ///</summary>
        public virtual void SubmitChanges()
        {
            var queryColleciton = (QueryCollection<T>)this.collection;

            BucketImpl bucket = BucketImpl<T>.NewInstance.Describe();

            IList<QueryObject<T>> tobeDeletedList = new List<QueryObject<T>>();

            foreach (QueryObject<T> item in queryColleciton.Objects)
            {
                try
                {
                    SavingEventArgs e = new SavingEventArgs();
                    if (item.IsNewlyAdded)
                    {
                        e.Action = SaveAction.Insert;
                        Kiss.QueryObject.OnSaving(item.ReferringObject, e);
                        if (e.Cancel)
                            continue;

                        bool added = PerformChange(bucket, item, this.AddItem);

                        if (added)
                        {
                            item.IsNewlyAdded = false;
                            // cache the item to track for update.
                            (item as IVersionItem).Commit();
                            Kiss.QueryObject.OnSaved(item.ReferringObject, SaveAction.Insert);
                        }
                        else
                        {
                            RaiseError(bucket.Name + " " + "add failed");
                        }

                    }
                    else if (item.IsDeleted)
                    {
                        e.Action = SaveAction.Delete;
                        Kiss.QueryObject.OnSaving(item.ReferringObject, e);
                        if (e.Cancel) continue;

                        if (PerformChange(bucket, item, this.RemoveItem))
                        {
                            tobeDeletedList.Add(item);
                            Kiss.QueryObject.OnSaved(item.ReferringObject, SaveAction.Delete);
                        }
                        else
                        {
                            RaiseError(bucket.Name + " " + "delete failed");
                        }
                    }
                    else if (item.IsAltered)
                    {
                        e.Action = SaveAction.Update;
                        Kiss.QueryObject.OnSaving(item.ReferringObject, e);
                        if (e.Cancel) continue;

                        if (PerformChange(bucket, item, this.UpdateItem))
                        {
                            (item as IVersionItem).Commit();
                            Kiss.QueryObject.OnSaved(item.ReferringObject, SaveAction.Update);
                        }
                        else
                        {
                            (item as IVersionItem).Revert();
                            RaiseError(bucket.Name + " " + "update failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Clear();
                    (this as IDisposable).Dispose();
                    throw new LinqException(ex.Message, ex);
                }
            }
            // delete the removed items. 
            foreach (var queryObject in tobeDeletedList)
            {
                queryColleciton.Objects.Remove(queryObject);
            }
        }

        internal void VisitExpression<S>(Expression expression, int level, TreeNode parentNode)
        {
            if (expression.NodeType == ExpressionType.Equal
                || expression.NodeType == ExpressionType.LessThan
                || expression.NodeType == ExpressionType.GreaterThan
                || expression.NodeType == ExpressionType.GreaterThanOrEqual
                || expression.NodeType == ExpressionType.LessThanOrEqual
                || expression.NodeType == ExpressionType.NotEqual
                || (expression.NodeType == ExpressionType.Call))
            {

                bool singleOrExtensionCall = false;
                // for extension and single item call.
                if (Buckets.Current.SyntaxStack.Count == 0)
                {
                    parentNode = ProcessCurrentNode(parentNode, OperatorType.AND);
                    singleOrExtensionCall = true;
                }

                Buckets.Current.Level = level;
                Buckets.Current.CurrentExpessionType = expression.NodeType;
                Buckets.Current.SyntaxStack.Peek().Level = level;

                Buckets.Current.SyntaxStack.Push(new BucketImpl<T>.TreeNodeInfo
                {
                    OperatorType = OperatorType.NONE,
                    Id = Guid.NewGuid(),
                    ParentId = Buckets.Current.SyntaxStack.Peek().Id,
                });

                Buckets.Current.CurrentTreeNode = parentNode;

                // push the state.
                this.ProcessBinaryResult(expression);

                if (singleOrExtensionCall)
                {
                    Buckets.Current.SyntaxStack.Pop();
                }
            }
            else if (expression.NodeType == ExpressionType.AndAlso
                        || expression.NodeType == ExpressionType.And
                        || expression.NodeType == ExpressionType.Or
                        || expression.NodeType == ExpressionType.OrElse)
            {
                OperatorType opType = expression.NodeType == ExpressionType.AndAlso || expression.NodeType == ExpressionType.And ? OperatorType.AND : OperatorType.OR;

                TreeNode currentNode = ProcessCurrentNode(parentNode, opType);

                this.VisitExpression<S>(((BinaryExpression)expression).Left, level + 1, currentNode);
                // visit next node.   
                this.VisitExpression<S>(((BinaryExpression)expression).Right, level + 2, currentNode);

                Buckets.Current.SyntaxStack.Pop();
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression uExp = expression as UnaryExpression;
                VisitExpression<S>(uExp.Operand, level, parentNode);
            }
            else if (expression is LambdaExpression)
            {
                VisitExpression<S>(((LambdaExpression)expression).Body, level, parentNode);
            }

            else if (expression is ParameterExpression)
            {
                if (expression.Type == typeof(T))
                {
                    // nothing do here
                }
            }
            else if (expression is ConstantExpression)
            {
                ConstantExpression constantExpression = expression as ConstantExpression;

                if (this.currentExpression is MethodCallExpression)
                {
                    object value = constantExpression.Value;
                    if (value != null)
                    {
                        FillOptionalBucketItems(value, this.currentExpression);
                    }
                }
            }
            else if (expression is MemberExpression)
            {
                MemberExpression mExp = expression as MemberExpression;

                if (this.currentExpression is MethodCallExpression)
                {
                    MethodCallExpression mCall = this.currentExpression as MethodCallExpression;

                    if (mCall.Method.Name == CallType.Orderby || mCall.Method.Name == CallType.Orderbydesc || mCall.Method.Name == CallType.ThenBy)
                    {
                        string orderBy = string.Empty;

                        // make sure its a constant expression.
                        if (mExp.Expression is ConstantExpression)
                        {
                            object value = mCall.GetValueFromExpression();

                            if (value != null)
                                orderBy = Convert.ToString(value);
                        }
                        else
                        {
                            orderBy = Buckets.Current.Items[mExp.Member.Name].Name;
                        }

                        if (!string.IsNullOrEmpty(orderBy))
                        {
                            Buckets.Current.OrderByItems.Add(new Bucket.OrderByInfo(orderBy, Buckets.Current.IsAsc));
                        }
                    }
                }
            }

        }

        private void FillOptionalBucketItems(object value, Expression expression)
        {
            if (expression is MethodCallExpression)
            {
                MethodCallExpression mCall = this.currentExpression as MethodCallExpression;

                if (mCall.Method.Name == CallType.Take)
                {
                    Buckets.Current.ItemsToTake = (int)value;
                }
                else if (mCall.Method.Name == CallType.Skip)
                {
                    Buckets.Current.ItemsToSkip = (int)value;
                }
                else if (mCall.Method.Name == CallType.Orderby || mCall.Method.Name == CallType.Orderbydesc || mCall.Method.Name == CallType.ThenBy)
                {
                    Buckets.Current.OrderByItems.Add(new Bucket.OrderByInfo(Convert.ToString(value), Buckets.Current.IsAsc));
                }
            }
        }

        private TreeNode ProcessCurrentNode(TreeNode parentNode, OperatorType opType)
        {
            Buckets.Current.SyntaxStack.Push(new BucketImpl<T>.TreeNodeInfo
            {
                OperatorType = opType,
                Id = Guid.NewGuid(),
                ParentId = Buckets.Current.SyntaxStack.Count > 0 ? Buckets.Current.SyntaxStack.Peek().Id : Guid.Empty,
            });


            Bucket bucket = Buckets.Current;
            TreeNode currentNode = null;

            var child = new TreeNode();

            if (parentNode == null && bucket.CurrentNode.Nodes.Count == 2)
            {
                parentNode = bucket.CurrentNode;
                // child becomes parent.
                child.Id = Guid.NewGuid();
                parentNode.ParentId = child.Id;
                child.RootImpl = opType;
                child.Nodes.Add(new TreeNode.Node { Value = parentNode });
                bucket.CurrentNode = child;
                currentNode = child;
            }
            else if (parentNode != null)
            {
                child.Id = Buckets.Current.SyntaxStack.Peek().Id;
                child.RootImpl = opType;
                child.ParentId = parentNode.Id;
                // make it a child.
                parentNode.Nodes.Add(new TreeNode.Node { Value = child });
                currentNode = child;
            }
            else
            {
                bucket.CurrentNode.Id = Buckets.Current.SyntaxStack.Peek().Id;
                bucket.CurrentNode.RootImpl = opType;
                currentNode = bucket.CurrentNode;
            }
            return currentNode;
        }

        private BucketImpls<T> Buckets
        {
            get
            {
                if (queryObjects == null)
                    queryObjects = new BucketImpls<T>();
                return queryObjects;
            }
        }

        private void ProcessBinaryResult(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;

            if (binaryExpression != null)
            {
                if (binaryExpression.Left is MemberExpression)
                {
                    ExtractDataFromExpression(Buckets.Current, binaryExpression.Left, binaryExpression.Right);//expression.Left);
                }
                else
                {
                    // this is needed for enum comparsion. i.e. there is Convert(ph.something) in the expression.
                    if (binaryExpression.Left is UnaryExpression)
                    {
                        UnaryExpression uExp = (UnaryExpression)binaryExpression.Left;

                        if (uExp.Operand is MethodCallExpression)
                        {
                            var methodCallExpression = (MethodCallExpression)uExp.Operand;

                            FillBucketFromMethodCall(binaryExpression, methodCallExpression);
                        }
                        else
                        {
                            ExtractDataFromExpression(Buckets.Current, uExp.Operand, binaryExpression.Right);
                        }

                    }
                    else if (binaryExpression.Left is MethodCallExpression)
                    {
                        MethodCallExpression methodCallExpression = (MethodCallExpression)binaryExpression.Left;
                        // if there are two arguments for name and value.
                        if (methodCallExpression.Arguments.Count > 1)
                        {
                            ExtractDataFromExpression(Buckets.Current, methodCallExpression.Arguments[0],
                                                      methodCallExpression.Arguments[1]);
                        }
                        else
                        {
                            // try a method call fill up.
                            FillBucketFromMethodCall(binaryExpression, methodCallExpression);
                        }
                    }
                }
            }
            else
            {
                var methodCallExpression = expression as MethodCallExpression;

                if (methodCallExpression != null)
                {
                    // try a method call fill up.
                    FillBucketFromMethodCall(binaryExpression, methodCallExpression);
                }

            }
        }

        private void FillBucketFromMethodCall(BinaryExpression expression, MethodCallExpression methodCallExpression)
        {
            BucketImpl bucketImpl = Buckets.Current;
            bucketImpl.IsDirty = true;
            bucketImpl.ClauseItemCount = bucketImpl.ClauseItemCount + 1;
            Buckets.Current.SyntaxStack.Pop();

            if (expression != null)
            {
                object value = Expression.Lambda(expression.Right).Compile().DynamicInvoke();

                var leafItem = new BucketItem
                {
                    Name = methodCallExpression.Method.Name,
                    Method = new BucketItem.ExtenderMethod
                    {
                        Name = methodCallExpression.Method.Name,
                        Arguments = methodCallExpression.Arguments,
                        Method = methodCallExpression.Method
                    }
                };

                leafItem.Values.Add(new BucketItem.QueryCondition(value, bucketImpl.Relation));
                bucketImpl.CurrentTreeNode.Nodes.Add(new TreeNode.Node() { Value = leafItem });
            }
            else
            {
                // method
                string methodName = methodCallExpression.Method.Name;
                if (methodName == "Contains" || methodName == "StartsWith" || methodName == "EndsWith")
                {
                    bool islike = methodCallExpression.Object.NodeType == ExpressionType.MemberAccess &&
                        ((MemberExpression)methodCallExpression.Object).Expression.NodeType == ExpressionType.Parameter;

                    if (islike)
                    {
                        MemberExpression memberExp = ((MemberExpression)(methodCallExpression.Object));
                        string memberName = memberExp.Member.Name;
                        if (bucketImpl.Items.ContainsKey(memberName))
                        {
                            object val = null;
                            if (methodCallExpression.Arguments[0] is ConstantExpression)
                                val = ((ConstantExpression)methodCallExpression.Arguments[0]).Value;
                            else
                                val = Expression.Lambda(methodCallExpression.Arguments[0]).Compile().DynamicInvoke();

                            var leafItem = new BucketItem
                            {
                                DeclaringObjectType = memberExp.Member.DeclaringType,
                                Name = bucketImpl.Items[memberName].Name,
                                ProperyName = bucketImpl.Items[memberName].ProperyName,
                                PropertyType = bucketImpl.Items[memberName].PropertyType,
                                Unique = bucketImpl.Items[memberName].Unique,
                                Child = bucketImpl.Items[memberName].Child
                            };

                            string v = string.Empty;
                            if (val != null)
                                v = val.ToString();

                            if (methodName == "StartsWith")
                                v = string.Concat(v, "%");
                            else if (methodName == "EndsWith")
                                v = string.Concat("%", v);
                            else
                                v = string.Concat("%", v, "%");

                            leafItem.Values.Add(new BucketItem.QueryCondition(v, RelationType.Like));

                            bucketImpl.CurrentTreeNode.Nodes.Add(new TreeNode.Node { Value = leafItem });
                        }
                    }
                    else
                    {
                        var value = Expression.Lambda(methodCallExpression.Object).Compile().DynamicInvoke() as IList;

                        if (value != null && value.Count > 0)
                        {
                            //if (value.Count == 0)
                            //{
                            //    Clear();
                            //    (this as IDisposable).Dispose();
                            //    throw new LinqException("list is empty.");
                            //}

                            var memberExpression = methodCallExpression.Arguments[0] as MemberExpression;

                            if (memberExpression != null)
                            {
                                string memberName = memberExpression.Member.Name;

                                if (bucketImpl.Items.ContainsKey(memberName))
                                {
                                    var leafItem = new BucketItem
                                    {
                                        DeclaringObjectType = memberExpression.Member.DeclaringType,
                                        Name = bucketImpl.Items[memberName].Name,
                                        ProperyName = bucketImpl.Items[memberName].ProperyName,
                                        PropertyType = bucketImpl.Items[memberName].PropertyType,
                                        Unique = bucketImpl.Items[memberName].Unique,
                                        Child = bucketImpl.Items[memberName].Child
                                    };

                                    if (value.Count == 1)
                                    {
                                        leafItem.Values.Add(new BucketItem.QueryCondition(value[0], RelationType.Equal));
                                    }
                                    else
                                    {
                                        foreach (object item in value)
                                        {
                                            leafItem.Values.Add(new BucketItem.QueryCondition(item, RelationType.Contains));
                                        }
                                    }

                                    bucketImpl.CurrentTreeNode.Nodes.Add(new TreeNode.Node { Value = leafItem });
                                }
                            }
                        }
                    }
                }
                else if (methodName == "Equals")
                {
                    MemberExpression memberExp = ((MemberExpression)(methodCallExpression.Object));
                    string memberName = memberExp.Member.Name;
                    if (bucketImpl.Items.ContainsKey(memberName))
                    {
                        object val = null;
                        if (methodCallExpression.Arguments[0] is ConstantExpression)
                            val = ((ConstantExpression)methodCallExpression.Arguments[0]).Value;
                        else
                            val = Expression.Lambda(methodCallExpression.Arguments[0]).Compile().DynamicInvoke();

                        // bug fix
                        bucketImpl.Items[memberName].Values.Add(new BucketItem.QueryCondition(val, RelationType.Equal));

                        var leafItem = new BucketItem
                        {
                            DeclaringObjectType = memberExp.Member.DeclaringType,
                            Name = bucketImpl.Items[memberName].Name,
                            ProperyName = bucketImpl.Items[memberName].ProperyName,
                            PropertyType = bucketImpl.Items[memberName].PropertyType,
                            Unique = bucketImpl.Items[memberName].Unique,
                            Child = bucketImpl.Items[memberName].Child
                        };

                        leafItem.Values.Add(new BucketItem.QueryCondition(val, RelationType.Equal));

                        bucketImpl.CurrentTreeNode.Nodes.Add(new TreeNode.Node { Value = leafItem });
                    }
                }
            }
        }

        private void ExtractDataFromExpression(BucketImpl bucket, Expression left, Expression right)
        {
            object value = Expression.Lambda(right).Compile().DynamicInvoke();

            MemberExpression memberExpression = (MemberExpression)left;
            string originalMembername = memberExpression.Member.Name;

            PropertyInfo targetProperty = null;
            // for nested types.
            if (!IsDeclaringType(memberExpression.Member, typeof(T)))
            {
                Type targetType = memberExpression.Member.DeclaringType;

                while (true)
                {
                    if (targetType.DeclaringType == null || targetType.DeclaringType == typeof(T))
                        break;
                    targetType = targetType.DeclaringType;
                }

                PropertyInfo[] infos = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                targetProperty = FindTargetPropertyWhereUsed(infos, targetType);

                object nestedObj = Activator.CreateInstance(targetType);

                if (targetProperty.CanWrite)
                {
                    PropertyInfo property = nestedObj.GetType().GetProperty(memberExpression.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                    if (property == null)
                    {
                        // go deep find n.
                        object nestedChildObject = FindDeepObject(nestedObj, targetType, memberExpression.Member.Name);

                        nestedChildObject.GetType()
                            .GetProperty(memberExpression.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                            .SetValue(nestedChildObject, value, null);

                    }
                    else
                    {
                        property.SetValue(nestedObj, value, null);
                    }

                    // reset the value.
                    value = nestedObj;
                }
            }
            else
            {
                targetProperty = typeof(T).GetProperty(originalMembername, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }

            object[] attr = targetProperty.GetCustomAttributes(typeof(IgnoreAttribute), true);

            if (attr.Length == 0)
            {
                if (targetProperty.CanRead)
                {
                    bucket.IsDirty = true;
                    FillBucket(bucket, targetProperty, value, memberExpression.Expression);
                }
            }
        }

        private bool IsDeclaringType(MemberInfo mi, Type t)
        {
            Type diclaringType = mi.DeclaringType;

            do
            {
                if (diclaringType == t)
                    return true;

                t = t.BaseType;
            } while (t.Name != "Object");

            return false;
        }

        private PropertyInfo FindTargetPropertyWhereUsed(PropertyInfo[] infos, Type targetType)
        {
            IList<PropertyInfo> compositeProperties = new List<PropertyInfo>();

            foreach (PropertyInfo property in infos)
            {
                if (!property.PropertyType.IsPrimitive && property.PropertyType.FullName.IndexOf("System") == -1 && !property.PropertyType.IsEnum)
                {
                    compositeProperties.Add(property);
                }

                if (property.PropertyType == targetType)
                {
                    return property;
                }
            }
            // try only if no properties found on the first step.
            foreach (PropertyInfo info in compositeProperties)
            {
                return FindTargetPropertyWhereUsed(info.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), targetType);
            }

            return null;
        }

        private static object FindDeepObject(object nestedObject, Type baseType, string propName)
        {
            PropertyInfo[] infos = baseType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (var info in infos)
            {
                if (info.Name == propName)
                {
                    return nestedObject;
                }
                else
                {
                    if (info.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Count() > 0)
                    {
                        try
                        {
                            object tobeNested = info.GetValue(nestedObject, null);

                            if (tobeNested == null)
                            {
                                tobeNested = Activator.CreateInstance(info.PropertyType);
                                if (info.CanWrite)
                                {
                                    info.SetValue(nestedObject, tobeNested, null);
                                }
                            }
                            return FindDeepObject(tobeNested, info.PropertyType, propName);
                        }
                        catch
                        {
                            // skip
                        }
                    }
                }
            }
            return null;
        }

        private void FillBucket(BucketImpl bucket, PropertyInfo info, object value, Expression expression)
        {
            bucket.ClauseItemCount = bucket.ClauseItemCount + 1;

            Buckets.Current.SyntaxStack.Pop();

            string[] parts = expression.ToString().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            Bucket current = bucket;
            bool nested = false;

            for (int index = 1; index < parts.Length; index++)
            {
                Type propertyType = current.Items[parts[index]].PropertyType;

                if (!propertyType.IsPrimitive
                    && propertyType.FullName.IndexOf("System") == -1
                    && !propertyType.IsEnum)
                {
                    if (current.Items[parts[index]].Child == null)
                    {
                        current.Items[parts[index]].Child = BucketImpl.NewInstance(propertyType).Describe();
                    }
                    // move on.
                    current = current.Items[parts[index]].Child;
                    nested = true;
                }
            }


            BucketItem item = null;

            if (current != null && nested)
            {
                foreach (PropertyInfo property in info.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    object targetValue = property.GetValue(value, null);

                    if (targetValue != null && !targetValue.EqualsDefault(property.Name, value))
                    {
                        current.Items[property.Name].Values.Add(new BucketItem.QueryCondition(targetValue, bucket.Relation));
                    }
                }

            }

            item = parts.Length > 1 ? bucket.Items[parts[1]] : bucket.Items[info.Name];

            BucketItem leafItem;

            if (item.Child != null)
            {
                BucketItem i = item.GetActiveItem();

                leafItem = new BucketItem
                {
                    DeclaringObjectType = i.DeclaringObjectType,
                    Name = i.Name,
                    ProperyName = i.ProperyName,
                    PropertyType = info.PropertyType,
                    Unique = i.Unique,
                    Child = i.Child
                };

                leafItem.Values.Add(new BucketItem.QueryCondition(i.Value, bucket.Relation));
            }
            else
            {
                // for getting the values directly.
                // add it to the bucket condition list.
                item.Values.Add(new BucketItem.QueryCondition(value, bucket.Relation));

                leafItem = new BucketItem
                {
                    DeclaringObjectType = item.DeclaringObjectType,
                    Name = item.Name,
                    ProperyName = item.ProperyName,
                    PropertyType = info.PropertyType,
                    Unique = item.Unique
                };
                leafItem.Values.Add(new BucketItem.QueryCondition(value, bucket.Relation));
            }
            bucket.CurrentTreeNode.Nodes.Add(new TreeNode.Node() { Value = leafItem });
        }

        private static bool PerformChange(Bucket bucket, IQueryObjectImpl item, ActualMethodHandler callback)
        {
            bool success = true;
            // copy item
            bucket = item.FillBucket(bucket);

            if (callback != null)
            {
                success = callback(bucket);
            }

            if (success && !item.IsDeleted)
                item.FillObject(bucket);

            return success;
        }

        private static void RaiseError(string message)
        {
            new LinqException(message);
        }

        private void ProcessItem(BucketImpl item)
        {
            if (!item.IsAlreadyProcessed)
            {
                try
                {
                    bool uniqueCall = item.UniqueProperties.Length > 0;

                    foreach (string property in item.UniqueProperties)
                    {
                        // check if any of the unique item field is queried.
                        uniqueCall
                            &= (item.Items[property].Values.Count == 1 &&
                                item.Items[property].RelationType == RelationType.Equal);
                    }

                    if (uniqueCall && item.ClauseItemCount == 1)
                    {
                        T obj = GetItem(item);

                        if (obj != null)
                        {
                            if (obj.Equals(defaultItem))
                            {
                                // backward compatible
                                SelectItem(item, this.collection);
                            }
                            else
                            {
                                this.Add(obj);
                            }
                        }
                    }
                    else
                    {
                        SelectItem(item, this.collection);
                    }
                }
                catch (Exception ex)
                {
                    // clear all
                    Clear();
                    (this as IDisposable).Dispose();
                    throw new LinqException(ex.Message, ex);
                }
                item.IsAlreadyProcessed = true;
            }
        }

        private delegate bool ActualMethodHandler(Bucket bucket);

        private Expression currentExpression;
        private BucketImpls<T> queryObjects;
        private readonly T defaultItem = (T)Activator.CreateInstance(typeof(T));
        protected IModify<T> collection;
        private object projectedQuery;
    }

    /// <summary>
    /// Represents the relational query operator equavalent.
    /// </summary>
    public enum RelationType
    {
        /// <summary>
        /// Eqavalent of "=="
        /// </summary>
        Equal = 0,
        /// <summary>
        /// Eqavalent of ">"
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Eqavalent of <![CDATA[ < ]]>
        /// </summary>
        LessThan,
        /// <summary>
        /// Eqavalent of ">="
        /// </summary>
        GreaterThanEqual,
        /// <summary>
        /// Eqavalent of <![CDATA[<=]]>
        /// </summary>
        LessThanEqual,
        /// <summary>
        /// Eqavalent of "!="
        /// </summary>
        NotEqual,
        /// <summary>
        /// Defines the Contains operation in expression.
        /// </summary>
        Contains,
        /// <summary>
        /// Eqavalent of "like"
        /// </summary>
        Like,
        /// <summary>
        /// Default value for first where clause item
        /// </summary>
        NotApplicable

    }

    internal class CallType
    {
        internal const string Join = "Join";
        internal const string Take = "Take";
        internal const string Skip = "Skip";
        internal const string Where = "Where";
        internal const string Select = "Select";
        internal const string Orderby = "OrderBy";
        internal const string ThenBy = "ThenBy";
        internal const string Orderbydesc = "OrderByDescending";
    }
}
