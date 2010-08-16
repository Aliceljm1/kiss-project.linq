using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Text;
using Kiss.Linq.Fluent;
using Kiss.Linq.Sql.DataBase;
using Kiss.Utils;

namespace Kiss.Linq.Sql
{
    /// <summary>
    /// sql query
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlQuery<T> : Query<T>, IKissQueryable<T>
        where T : IQueryObject, new()
    {
        #region fields

        protected ConnectionStringSettings connectionStringSettings;

        private DatabaseContext _dataContext = null;
        public DatabaseContext DataContext
        {
            get
            {
                if (_dataContext == null)
                {
                    _dataContext = new DatabaseContext(connectionStringSettings, typeof(T));
                }
                return _dataContext;
            }
        }

        public bool EnableQueryEvent { get; set; }

        #endregion

        #region ctor

        public SqlQuery(ConnectionStringSettings connectionStringSettings)
        {
            this.connectionStringSettings = connectionStringSettings;
            this.EnableQueryEvent = true;
        }

        #endregion

        public void SubmitChanges(bool batch)
        {
            if (batch)
            {
                var queryColleciton = (QueryCollection<T>)this.collection;

                if (queryColleciton.Objects.Count == 0)
                    return;

                BucketImpl bucket = BucketImpl<T>.NewInstance.Describe();

                try
                {
                    PerformChange(bucket, queryColleciton.Objects);
                }
                catch (Exception ex)
                {
                    throw new LinqException("BATCH SubmitChanges ERROR!  " + ex.Message, ex);
                }

                queryColleciton.Clear();
            }
            else
            {
                base.SubmitChanges();
            }
        }

        #region override

        protected override bool AddItem(IBucket bucket)
        {
            return ExecuteReaderAndFillBucket(bucket,
                FluentBucket.As(bucket).Translate(FormatMethod.AddItem, DataContext.FormatProvider));
        }

        protected override bool UpdateItem(IBucket bucket)
        {
            return ExecuteReaderAndFillBucket(bucket,
                FluentBucket.As(bucket).Translate(FormatMethod.UpdateItem, DataContext.FormatProvider));
        }

        protected override bool RemoveItem(IBucket bucket)
        {
            ExecuteOnly(FluentBucket.As(bucket).Translate(FormatMethod.RemoveItem, DataContext.FormatProvider));

            return true;
        }

        protected override T GetItem(IBucket bucket)
        {
            string sql = FluentBucket.As(bucket).Translate(FormatMethod.GetItem, DataContext.FormatProvider);

            if (EnableQueryEvent)
            {
                DatabaseContext.QueryEventArgs e = new DatabaseContext.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql
                };
                DataContext.OnPreQuery(e);

                if (e.Result != null)
                    return (T)e.Result;
            }

            T result = ExecuteSingle(sql,
               bucket);

            if (EnableQueryEvent)
            {
                DataContext.OnAfterQuery(new DatabaseContext.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql,
                    Result = result
                });
            }

            return result;
        }

        protected override void SelectItem(IBucket bucket, IModify<T> items)
        {
            string sql = FluentBucket.As(bucket).Translate(FormatMethod.Process, DataContext.FormatProvider);

            if (EnableQueryEvent)
            {
                DatabaseContext.QueryEventArgs e = new DatabaseContext.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql
                };
                DataContext.OnPreQuery(e);

                if (e.Result != null)
                {
                    AddRange(e.Result as List<T>);
                    return;
                }
            }

            FillObject(bucket,
                sql,
                items,
                bucket.Items);

            if (EnableQueryEvent)
            {
                DataContext.OnAfterQuery(new DatabaseContext.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql,
                    Result = ((QueryCollection<T>)collection).Items
                });
            }
        }

        #endregion

        #region Execute Sql

        private bool ExecuteMultipleStatements(IBucket bucket, string sql)
        {
            bool status = false;

            //Check if multiple queries need to be executed
            if (sql.Contains(";"))
            {
                //Parse the string into seperate queries and executed them.
                string[] delimiters = new string[] { ";" };
                string[] queries = sql.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sqlQuery in queries)
                {
                    status = ExecuteReaderAndFillBucket(bucket, sqlQuery);
                }
            }
            else
            {
                status = ExecuteReaderAndFillBucket(bucket, sql);
            }
            return status;
        }

        private int ExecuteOnly(string sql)
        {
            return DataContext.ExecuteNonQuery(CommandType.Text, sql);
        }

        private bool ExecuteReaderAndFillBucket(IBucket bucket, string sql)
        {
            using (IDataReader rdr = DataContext.ExecuteReader(CommandType.Text, sql))
            {
                if (rdr.RecordsAffected == 0)
                    return false;

                while (rdr.Read())
                {
                    FluentBucket.As(bucket).For.EachItem.Process(delegate(BucketItem item)
                    {
                        object obj = FetchDataReader(bucket, rdr, item.Name, item.PropertyType);
                        if (obj != null)
                            item.Value = obj;
                    });
                }

                rdr.Close();
            }

            return true;
        }

        private T ExecuteSingle(string sql, IBucket item)
        {
            IDictionary<string, BucketItem> bItems = item.Items;

            using (IDataReader rdr = DataContext.ExecuteReader(CommandType.Text, sql))
            {
                T obj = default(T);

                if (rdr.Read())
                {
                    obj = Activator.CreateInstance<T>();

                    Type t = typeof(T);
                    foreach (string key in bItems.Keys)
                    {
                        BucketItem bucketItem = bItems[key];

                        object o = FetchDataReader(item, rdr, bucketItem.Name, bucketItem.PropertyType);

                        if (o == null)
                            continue;

                        PropertyInfo pi = t.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly );
                        if (pi != null && pi.CanWrite)
                            pi.SetValue(obj,
                                o,
                                null);
                    }
                }
                rdr.Close();
                return obj;
            }
        }

        private static object FetchDataReader(IBucket item, IDataReader rdr, string key, Type targetType)
        {
            object o;

            try
            {
                o = rdr[key];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new LinqException(string.Format("column '{0}' doesn't exist in table '{1}'.", key, item.Name),
                    ex);
            }

            if (o is DBNull)
                return null;

            return TypeConvertUtil.ConvertTo(o, targetType);
        }

        private void FillObject(IBucket bucket, string sql, IModify<T> items, IDictionary<string, BucketItem> bItems)
        {
            using (IDataReader rdr = DataContext.ExecuteReader(CommandType.Text, sql))
            {
                while (rdr.Read())
                {
                    var item = new T();

                    var type = typeof(T);

                    foreach (string key in bItems.Keys)
                    {
                        BucketItem bucketItem = bItems[key];
                        PropertyInfo info = type.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

                        if (info != null && info.CanWrite)
                        {
                            object o = FetchDataReader(bucket, rdr, bucketItem.Name, bucketItem.PropertyType);
                            if (o == null)
                                continue;

                            info.SetValue(item
                                , o
                                , null);
                        }
                    }
                    items.Add(item);
                }

                rdr.Close();
            }
        }

        #endregion

        #region helper

        private void PerformChange(Bucket bucket, IList<QueryObject<T>> items)
        {
            StringBuilder sql = new StringBuilder();

            // copy item
            foreach (var item in items)
            {
                bucket = item.FillBucket(bucket);

                if (item.IsNewlyAdded)
                    sql.Append(FluentBucket.As(bucket).Translate(FormatMethod.BatchAdd, DataContext.FormatProvider));
                else if (item.IsDeleted)
                    sql.Append(FluentBucket.As(bucket).Translate(FormatMethod.BatchRemove, DataContext.FormatProvider));
                else if (item.IsAltered)
                    sql.Append(FluentBucket.As(bucket).Translate(FormatMethod.BatchUpdate, DataContext.FormatProvider));
            }

            if (sql.Length > 0)
            {
                ExecuteOnly(sql.ToString());

                Kiss.QueryObject.OnBatch(typeof(T));
            }
        }

        #endregion
    }
}
