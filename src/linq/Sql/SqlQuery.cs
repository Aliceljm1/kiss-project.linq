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
    public class SqlQuery<T> : Query<T>, ILinqContext<T>
        where T : IQueryObject, new()
    {
        #region fields

        protected KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> connectionStringSettings;

        public bool EnableQueryEvent { get; set; }

        private IDbTransaction transaction;
        public IDbTransaction Transaction { get { return transaction ?? TransactionScope.Transaction; } set { transaction = value; } }

        #endregion

        #region ctor

        public SqlQuery(ConnectionStringSettings css)
            : this(new KeyValuePair<ConnectionStringSettings, ConnectionStringSettings>(css, css))
        {

        }

        public SqlQuery(KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> connectionStringSettings)
            : this(connectionStringSettings, true)
        {
        }

        public SqlQuery(KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> connectionStringSettings, bool enableQueryEvent)
        {
            this.connectionStringSettings = connectionStringSettings;
            this.EnableQueryEvent = enableQueryEvent;
        }

        #endregion

        public override void SubmitChanges()
        {
            SubmitChanges(false);
        }

        public void SubmitChanges(bool batch)
        {
            Validation.ValidationManager vm = new Validation.ValidationManager<T>();

            var queryColleciton = (QueryCollection<T>)this.collection;

            foreach (var item in queryColleciton.Objects)
            {
                if (!item.IsDeleted && !vm.IsValid(item.ReferringObject))
                    throw new Validation.ValidationException(vm.GetValidationErrorContent());
            }

            if (batch)
            {
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
                finally
                {
                    queryColleciton.Clear();
                }
            }
            else
            {
                base.SubmitChanges();
            }
        }

        #region override

        protected override bool AddItem(IBucket bucket)
        {
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Value, typeof(T));

            return ExecuteReaderAndFillBucket(dc,
                bucket,
                Translate(bucket, FormatMethod.AddItem, dc.FormatProvider));
        }

        protected override bool UpdateItem(IBucket bucket)
        {
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Value, typeof(T));

            return ExecuteReaderAndFillBucket(dc,
                bucket,
                Translate(bucket, FormatMethod.UpdateItem, dc.FormatProvider));
        }

        protected override bool RemoveItem(IBucket bucket)
        {
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Value, typeof(T));

            ExecuteOnly(dc,
                Translate(bucket, FormatMethod.RemoveItem, dc.FormatProvider));

            return true;
        }

        protected override T GetItem(IBucket bucket)
        {
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Key, typeof(T));

            string sql = Translate(bucket, FormatMethod.GetItem, dc.FormatProvider);

            if (EnableQueryEvent)
            {
                Kiss.QueryObject.QueryEventArgs e = new Kiss.QueryObject.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql
                };
                Kiss.QueryObject.OnPreQuery(e);

                if (e.Result != null)
                    return (T)e.Result;
            }

            T result = ExecuteSingle(dc,
               sql,
               bucket);

            if (EnableQueryEvent)
            {
                Kiss.QueryObject.OnAfterQuery(new Kiss.QueryObject.QueryEventArgs()
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
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Key, typeof(T));

            string sql = Translate(bucket, FormatMethod.Process, dc.FormatProvider);

            if (EnableQueryEvent)
            {
                Kiss.QueryObject.QueryEventArgs e = new Kiss.QueryObject.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql
                };
                Kiss.QueryObject.OnPreQuery(e);

                if (e.Result != null)
                {
                    AddRange(e.Result as List<T>);
                    return;
                }
            }

            FillObject(dc,
                bucket,
                sql,
                items,
                bucket.Items);

            if (EnableQueryEvent)
            {
                Kiss.QueryObject.OnAfterQuery(new Kiss.QueryObject.QueryEventArgs()
                {
                    Type = typeof(T),
                    Sql = sql,
                    Result = ((QueryCollection<T>)collection).Items
                });
            }
        }

        #endregion

        #region Execute Sql

        private int ExecuteOnly(DatabaseContext dc, string sql)
        {
            return dc.ExecuteNonQuery(Transaction, sql);
        }

        private bool ExecuteReaderAndFillBucket(DatabaseContext dc, IBucket bucket, string sql)
        {
            using (IDataReader rdr = dc.ExecuteReader(Transaction, sql))
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

        private T ExecuteSingle(DatabaseContext dc, string sql, IBucket item)
        {
            IDictionary<string, BucketItem> bItems = item.Items;

            using (IDataReader rdr = dc.ExecuteReader(sql))
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

                        PropertyInfo pi = t.GetProperty(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
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

        private void FillObject(DatabaseContext dc, IBucket bucket, string sql, IModify<T> items, IDictionary<string, BucketItem> bItems)
        {
            using (IDataReader rdr = dc.ExecuteReader(sql))
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

        private void PerformChange(Bucket bucket, List<QueryObject<T>> list)
        {
            DatabaseContext dc = new DatabaseContext(connectionStringSettings.Value, typeof(T));

            // 1，首先处理删除和更新
            List<string> sqls = new List<string>();

            foreach (var item in list.FindAll((i) => { return !i.IsNewlyAdded; }))
            {
                Bucket bkt = item.FillBucket(bucket);

                if (item.IsNewlyAdded)
                    sqls.Add(Translate(bkt, FormatMethod.BatchAdd, dc.FormatProvider));
                else if (item.IsDeleted)
                    sqls.Add(Translate(bkt, FormatMethod.BatchRemove, dc.FormatProvider));
                else if (item.IsAltered)
                    sqls.Add(Translate(bkt, FormatMethod.BatchUpdate, dc.FormatProvider));
            }

            if (sqls.Count > 0)
            {
                int pc = (int)Math.Ceiling(sqls.Count / 100.0);

                for (int i = 0; i < pc; i++)
                {
                    ExecuteOnly(dc, sqls.GetRange(i * 100, Math.Min(sqls.Count - i * 100, 100)).Join(string.Empty));
                }
            }

            // 2，处理批量添加
            dc.BulkCopy<T>(bucket, list.FindAll((i) => { return i.IsNewlyAdded; }));

            Kiss.QueryObject.OnBatch(typeof(T));
        }

        public static string Translate(IBucket bucket, FormatMethod method, IFormatProvider formatProvider)
        {
            formatProvider.Initialize(bucket);

            string selectorString = GetFormatString(method, formatProvider);

            StringBuilder builder = new StringBuilder(selectorString);

            foreach (string format in StringUtil.GetAntExpressions(selectorString))
            {
                builder.Replace("${" + format + "}", formatProvider.DefineString(format));
            }

            return builder.ToString();
        }

        private static string GetFormatString(FormatMethod method, IFormatProvider formatProvider)
        {
            string selectorString = string.Empty;

            switch (method)
            {
                case FormatMethod.Process:
                    selectorString = formatProvider.ProcessFormat();
                    break;
                case FormatMethod.GetItem:
                    selectorString = formatProvider.GetItemFormat();
                    break;
                case FormatMethod.AddItem:
                    selectorString = formatProvider.AddItemFormat();
                    break;
                case FormatMethod.UpdateItem:
                    selectorString = formatProvider.UpdateItemFormat();
                    break;
                case FormatMethod.RemoveItem:
                    selectorString = formatProvider.RemoveItemFormat();
                    break;
                case FormatMethod.BatchAdd:
                    selectorString = formatProvider.BatchAddItemFormat();
                    break;
                case FormatMethod.BatchAddValues:
                    selectorString = formatProvider.BatchAddItemValuesFormat();
                    break;
                case FormatMethod.BatchUpdate:
                    selectorString = formatProvider.BatchUpdateItemFormat();
                    break;
                case FormatMethod.BatchRemove:
                    selectorString = formatProvider.BatchRemoveItemFormat();
                    break;
                default:
                    break;
            }

            return selectorString;
        }

        #endregion
    }
}
