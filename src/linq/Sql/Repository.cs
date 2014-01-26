using Kiss.Config;
using Kiss.Linq.Fluent;
using Kiss.Linq.Sql.DataBase;
using Kiss.Plugin;
using Kiss.Query;
using Kiss.Repository;
using Kiss.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Kiss.Linq.Sql
{
    public class Repository<T, t> : Repository<T>, IRepository<T, t>, IRepository<T>
        where T : Obj<t>, new()
    {
        #region ctor

        /// <summary>
        /// ctor
        /// </summary>
        public Repository()
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="connectionStringSettings"></param>
        public Repository(ConnectionStringSettings connectionStringSettings)
            : base(connectionStringSettings)
        {
        }

        public Repository(string connstr_name)
            : base(connstr_name)
        {
        }

        #endregion

        public T Get(t id)
        {
            return Get(CreateContext(true), id);
        }

        public T Get(ILinqContext<T> context, t id)
        {
            if (id == null) return default(T);
            if (id is string && StringUtil.IsNullOrEmpty(id.ToString())) return default(T);
            if (object.Equals(id, default(t)))
                return default(T);

            return (from obj in context
                    where obj.Id.Equals(id)
                    select obj).FirstOrDefault();
        }

        public List<T> Gets(t[] ids)
        {
            return Gets(CreateContext(true), ids);
        }

        public List<T> Gets(ILinqContext<T> context, t[] ids)
        {
            if (ids.Length == 0)
                return new List<T>();

            List<t> idlist = new List<t>();

            // trim duplicated ids
            foreach (var item in ids)
            {
                if (idlist.Contains(item))
                    continue;

                idlist.Add(item);
            }

            List<T> list = (from q in context
                            where idlist.Contains(q.Id)
                            select q).ToList();

            list.Sort(delegate(T t1, T t2)
            {
                return idlist.IndexOf(t1.Id).CompareTo(idlist.IndexOf(t2.Id));
            });

            return list;
        }

        public void DeleteById(params t[] ids)
        {
            if (ids.Length == 0)
                return;

            using (ILinqContext<T> cx = CreateContext(false))
            {
                cx.Remove(Gets(cx, ids));

                cx.SubmitChanges();
            }
        }

        public List<T> Gets(string commaDelimitedIds)
        {
            return Gets(CreateContext(true), commaDelimitedIds);
        }

        public List<T> Gets(ILinqContext<T> context, string commaDelimitedIds)
        {
            List<t> ids = new List<t>();

            foreach (var str in StringUtil.CommaDelimitedListToStringArray(commaDelimitedIds))
            {
                ids.Add(TypeConvertUtil.ConvertTo<t>(str));
            }

            return Gets(context, ids.ToArray());
        }

        public T Save(NameValueCollection param, ConvertObj<T> converter)
        {
            t id = default(t);
            if (StringUtil.HasText(param["id"]))
                id = TypeConvertUtil.ConvertTo<t>(param["id"]);

            T obj;

            ILinqContext<T> context = CreateContext(false);

            if (object.Equals(id, default(t)))
            {
                obj = new T();
                context.Add(obj);
            }
            else
            {
                obj = Get(context, id);

                if (obj == null)// create a new record
                {
                    obj = new T();
                    obj.Id = id;
                    context.Add(obj, true);
                }
            }

            if (!converter(obj, param))
                return null;

            context.SubmitChanges(false);

            return obj;
        }

        public T Save(string param, ConvertObj<T> converter)
        {
            return Save(StringUtil.DelimitedEquation2NVCollection("&", param), converter);
        }
    }

    public class Repository<T> : Repository, IRepository<T>, IAutoStart where T : IQueryObject, new()
    {
        #region ctor

        /// <summary>
        /// ctor
        /// </summary>
        public Repository()
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="connectionStringSettings"></param>
        public Repository(ConnectionStringSettings connectionStringSettings)
        {
            ConnectionStringSettings = new KeyValuePair<ConnectionStringSettings, ConnectionStringSettings>(connectionStringSettings, connectionStringSettings);
        }

        public Repository(string connstr_name)
            : this(ConfigBase.GetConnectionStringSettings(connstr_name))
        {
        }

        #endregion

        /// <summary>
        /// create new linq query
        /// </summary>
        /// <returns></returns>
        public ILinqContext<T> CreateContext(bool enableQueryEvent)
        {
            return new SqlQuery<T>(ConnectionStringSettings, enableQueryEvent);
        }

        /// <summary>
        /// 获取对象列表
        /// </summary>
        /// <param name="qc"></param>
        /// <returns></returns>
        public List<T> Gets(QueryCondition q)
        {
            CheckQuery(q, ConnectionStringSettings.Key);

            new DatabaseContext(ConnectionStringSettings.Key, typeof(T));

            q.FireBeforeQueryEvent("Gets", ConnectionStringSettings.Key.ProviderName);

            if (string.IsNullOrEmpty(q.TableField))
                q.TableField = "*";

            if (q.PageSize == -1)
                q.PageSize = 20;

            string sql = string.Concat(q.TableName, q.WhereClause, q.PageIndex, q.PageSize, q.TotalCount, q.OrderByClause, q.TableField);
            foreach (var item in q.Parameters)
            {
                sql += item.Value;
            }

            Kiss.QueryObject.QueryEventArgs e = new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql
            };
            Kiss.QueryObject.OnPreQuery(e);

            if (e.Result != null)
                return e.Result as List<T>;

            List<T> list = new List<T>();

            using (IDataReader rdr = q.GetReader())
            {
                BucketImpl bucket = new BucketImpl<T>().Describe();

                DataTable schemaTable = rdr.GetSchemaTable();
                // set column:"ColumnName" to primaryKey
                schemaTable.PrimaryKey = new DataColumn[] { schemaTable.Columns[0] };

                while (rdr.Read())
                {
                    var item = new T();
                    var t = typeof(T);

                    FluentBucket.As(bucket).For.EachItem.Process(delegate(BucketItem bucketItem)
                    {
                        if (schemaTable.Rows.Contains(bucketItem.Name))
                            fillObject(rdr, item, t, bucketItem);
                    });

                    list.Add(item);
                }
            }

            Kiss.QueryObject.OnAfterQuery(new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql,
                Result = list
            });

            return list;
        }

        public DataTable GetDataTable(QueryCondition q)
        {
            CheckQuery(q, ConnectionStringSettings.Key);

            new DatabaseContext(ConnectionStringSettings.Key, typeof(T));

            q.FireBeforeQueryEvent("GetDataTable", ConnectionStringSettings.Key.ProviderName);

            if (string.IsNullOrEmpty(q.TableField))
                q.TableField = "*";

            if (q.PageSize == -1)
                q.PageSize = 20;

            string sql = string.Concat(q.TableName, q.WhereClause, q.PageIndex, q.PageSize, q.TotalCount, q.OrderByClause, q.TableField);

            foreach (var item in q.Parameters)
            {
                sql += item.Value;
            }

            Kiss.QueryObject.QueryEventArgs e = new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql
            };
            Kiss.QueryObject.OnPreQuery(e);

            if (e.Result != null)
                return e.Result as DataTable;

            DataTable dt = q.GetDataTable();

            if (dt.Columns.Contains("propertyname") && dt.Columns.Contains("propertyvalue"))
            {
                Queue<ExtendedAttributes> attrs = new Queue<ExtendedAttributes>();
                List<string> ext_columns = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    ExtendedAttributes ext = new ExtendedAttributes();
                    ext.SetData(row["propertyname"] is DBNull ? string.Empty : Convert.ToString(row["propertyname"])
                        , row["propertyvalue"] is DBNull ? string.Empty : Convert.ToString(row["propertyvalue"]));

                    attrs.Enqueue(ext);

                    foreach (string key in ext.Keys)
                    {
                        if (!ext_columns.Contains(key.ToLower()))
                            ext_columns.Add(key.ToLower());
                    }
                }

                ext_columns.RemoveAll((i) =>
                {
                    return dt.Columns.Contains(i);
                });

                foreach (string ext_column in ext_columns)
                {
                    dt.Columns.Add(ext_column);
                }

                foreach (DataRow row in dt.Rows)
                {
                    ExtendedAttributes ext = attrs.Dequeue();
                    foreach (string ext_column in ext_columns)
                    {
                        row[ext_column] = ext[ext_column];
                    }
                }
            }

            Kiss.QueryObject.OnAfterQuery(new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql,
                Result = dt
            });

            return dt;
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="qc"></param>
        /// <returns></returns>
        public int Count(QueryCondition q)
        {
            CheckQuery(q, ConnectionStringSettings.Key);

            new DatabaseContext(ConnectionStringSettings.Key, typeof(T));

            q.FireBeforeQueryEvent("Count", ConnectionStringSettings.Key.ProviderName);

            string sql = "count" + q.WhereClause + q.TableName;

            foreach (var item in q.Parameters)
            {
                sql += item.Value;
            }

            Kiss.QueryObject.QueryEventArgs e = new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql
            };
            Kiss.QueryObject.OnPreQuery(e);

            if (e.Result != null)
                return (int)e.Result;

            int count = q.GetRelationCount();

            Kiss.QueryObject.OnAfterQuery(new Kiss.QueryObject.QueryEventArgs()
            {
                Type = typeof(T),
                Sql = sql,
                Result = count
            });

            return count;
        }

        public List<T> GetsAll()
        {
            return GetsAll(CreateContext(true));
        }

        public List<T> GetsAll(ILinqContext<T> context)
        {
            return (from q in context
                    select q).ToList();
        }

        object IRepository.Gets(QueryCondition q)
        {
            return Gets(q);
        }

        private void CheckQuery(QueryCondition q, ConnectionStringSettings css)
        {
            if (q.ConnectionStringSettings == null)
                q.ConnectionStringSettings = css;

            string tablename = Kiss.QueryObject<T>.GetTableName();

            if (string.IsNullOrEmpty(q.TableName))
                q.TableName = tablename;
        }

        private static void fillObject(IDataReader rdr, T item, Type t, BucketItem bucketItem)
        {
            PropertyInfo info = t.GetProperty(bucketItem.ProperyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (info != null && info.CanWrite)
            {
                object o = null;

                int index = rdr.GetOrdinal(bucketItem.Name);
                if (index != -1)
                {
                    o = rdr[index];
                    if (!(o is DBNull))
                    {
                        o = TypeConvertUtil.ConvertTo(o, info.PropertyType);

                        if (o != null)
                            info.SetValue(item
                                , o
                                , null);
                    }
                }
            }
        }

        #region IAutoStart Members

        public void Start()
        {
            CreatedEventArgs e = new CreatedEventArgs(typeof(T));
            e.ConnectionStringSettings = ConnectionStringSettings;

            OnCreated(e);

            if (e.ConnectionStringSettings.Key != null && e.ConnectionStringSettings.Value != null)
                ConnectionStringSettings = e.ConnectionStringSettings;
        }

        #endregion

        public IWhere Where(string where, params object[] args)
        {
            return new WhereClause<T>(ConnectionStringSettings).Where(where, args);
        }
    }

    public class Repository
    {
        /// <summary>
        /// store READ and WRITE connection string as Key and Value
        /// </summary>
        public KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> ConnectionStringSettings { get; set; }

        public static event EventHandler<CreatedEventArgs> Created;

        protected void OnCreated(CreatedEventArgs e)
        {
            ConnectionStringSettings = LoadConn(e.ModelType);

            EventHandler<CreatedEventArgs> handler = Created;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private static Dictionary<Type, KeyValuePair<ConnectionStringSettings, ConnectionStringSettings>> _caches = new Dictionary<Type, KeyValuePair<ConnectionStringSettings, ConnectionStringSettings>>();

        private static KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> LoadConn(Type type)
        {
            if (_caches.ContainsKey(type))
                return _caches[type];

            lock (_caches)
            {
                if (_caches.ContainsKey(type))
                    return _caches[type];

                Kiss.Repository.RepositoryPluginSetting setting = PluginSettings.Get<RepositoryInitializer>() as Kiss.Repository.RepositoryPluginSetting;

                if (setting == null)
                    throw new ConfigException("cann't find RepositoryPlugin's config.");

                if (string.IsNullOrEmpty(setting.DefaultConn))
                    throw new ConfigException("default connection string cann't be empty.");

                ConnectionStringSettings css = ConfigBase.GetConnectionStringSettings(setting.DefaultConn);

                if (css == null)
                    throw new ConfigException(string.Format("cann't find default connection string setting. connection string name: ", setting.DefaultConn));

                ConnectionStringSettings css_master = css;

                if (!string.IsNullOrEmpty(setting.DefaultMasterConn))
                    css_master = ConfigBase.GetConnectionStringSettings(setting.DefaultMasterConn);

                if (css_master == null)
                    throw new ConfigException(string.Format("cann't find default MASTER connection string setting. connection string name: ", setting.DefaultConn));

                // save default connection string
                if (ConfigBase.DefaultConnectionStringSettings == null)
                    ConfigBase.DefaultConnectionStringSettings = css;

                string tablename = Kiss.QueryObject.GetTableName(type);

                foreach (var conn in setting.Conns)
                {
                    bool match = false;

                    foreach (string table in StringUtil.Split(conn.Key.Value, ",", true, true))
                    {
                        if (table.StartsWith("*") && tablename.EndsWith(table.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                            match = true;

                        if (!match && table.EndsWith("*") && tablename.StartsWith(table.Substring(0, table.Length - 1), StringComparison.InvariantCultureIgnoreCase))
                            match = true;

                        if (!match && table.StartsWith("*") && table.EndsWith("*") && tablename.ToLower().Contains(table.Substring(1, table.Length - 1).ToLower()))
                            match = true;

                        if (!match && string.Equals(table, tablename, StringComparison.InvariantCultureIgnoreCase))
                            match = true;

                        if (match)
                            break;
                    }

                    if (match)
                    {
                        css = Config.ConfigBase.GetConnectionStringSettings(conn.Key.Key);

                        if (css == null)
                            throw new ConfigException(string.Format("cann't find default connection string setting. connection string name: ", conn.Key.Key));

                        if (!string.IsNullOrEmpty(conn.Value["conn_master"]))
                        {
                            css_master = Config.ConfigBase.GetConnectionStringSettings(conn.Value["conn_master"]);

                            if (css_master == null)
                                throw new ConfigException(string.Format("cann't find default MASTER connection string setting. connection string name: ", setting.DefaultConn));
                        }
                        else
                        {
                            css_master = css;
                        }

                        break;
                    }
                }

                _caches[type] = new KeyValuePair<ConnectionStringSettings, ConnectionStringSettings>(css, css_master);

                return _caches[type];
            }
        }

        public class CreatedEventArgs : EventArgs
        {
            public KeyValuePair<ConnectionStringSettings, ConnectionStringSettings> ConnectionStringSettings { get; set; }

            public Type ModelType { get; private set; }

            public CreatedEventArgs(Type modelType)
            {
                ModelType = modelType;
            }
        }
    }
}
