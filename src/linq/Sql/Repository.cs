using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using Kiss.Config;
using Kiss.Linq.Fluent;
using Kiss.Query;
using Kiss.Utils;

namespace Kiss.Linq.Sql
{
    public class Repository<T, t> : Repository<T>, IRepository<T, t>, IRepository<T>
        where T : Obj<t>, new()
        where t : IEquatable<t>
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

        //protected GetCacheKey getCacheKey = delegate(t i) { return string.Format("{0}:{1}", typeof(T).Name.ToLower(), i.ToString()); };

        ///// <summary>
        ///// Fine-grained using id
        ///// </summary>
        //public bool CacheIdGranularity { get; set; }

        public virtual T Get(t id)
        {
            if (object.Equals(id, default(t)))
                return default(T);

            return (from obj in Query
                    where obj.Id.Equals(id)
                    select obj).FirstOrDefault();
        }

        public virtual List<T> Gets(t[] ids)
        {
            if (ids.Length == 0)
                return new List<T>();

            List<t> idlist = new List<t>(ids);

            List<T> list = (from obj in Query
                            where new List<t>(ids).Contains(obj.Id)
                            select obj).ToList();

            list.Sort(delegate(T t1, T t2)
            {
                return idlist.IndexOf(t1.Id).CompareTo(idlist.IndexOf(t2.Id));
            });

            return list;
        }

        public virtual void DeleteById(params t[] ids)
        {
            if (ids.Length == 0)
                return;

            foreach (t id in ids)
            {
                T obj = new T() { Id = id };

                Query.Add(obj);
                Query.Remove(obj);
            }

            Query.SubmitChanges(true);
        }

        public virtual List<T> Gets(string commaDelimitedIds)
        {
            return Gets(StringUtil.ToArray<string, t>(StringUtil.CommaDelimitedListToStringArray(commaDelimitedIds), delegate(string str)
            {
                return TypeConvertUtil.ConvertTo<t>(str);
            }));
        }

        public virtual T Save(NameValueCollection param, ConvertObj<T> converter)
        {
            t id = default(t);
            if (StringUtil.HasText(param["id"]))
                id = TypeConvertUtil.ConvertTo<t>(param["id"]);

            T obj;

            if (default(t).Equals(id))
            {
                obj = new T();
                Query.Add(obj);
            }
            else
            {
                Query.EnableQueryEvent = false;
                obj = Get(id);

                if (obj == null)
                    throw new ArgumentException(string.Format("{0} object not exist. Id:{1}", typeof(T).Name, id));
            }

            if (!converter(obj, param))
                return null;

            Query.SubmitChanges(false);

            return obj;
        }

        public virtual T Save(string param, ConvertObj<T> converter)
        {
            return Save(StringUtil.DelimitedEquation2NVCollection("&", param), converter);
        }
    }

    public class Repository<T> : Repository, IRepository<T>, IAutoStart where T : class, IQueryObject, new()
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
            ConnectionStringSettings = connectionStringSettings;
        }

        public Repository(string connstr_name)
            : this(ConfigBase.GetConnectionStringSettings(connstr_name))
        {
        }

        #endregion

        private ConnectionStringSettings _connectionStringSettings;
        public ConnectionStringSettings ConnectionStringSettings
        {
            get
            {
                if (_connectionStringSettings == null)
                    throw new LinqException("connectionString is not set!");

                return _connectionStringSettings;
            }
            set
            {
                _connectionStringSettings = value;
            }
        }

        private SqlQuery<T> _query;

        public ILinqQuery<T> Query
        {
            get
            {
                if (_query == null)
                    _query = CreateQuery();
                return _query;
            }
        }

        /// <summary>
        /// create new linq query
        /// </summary>
        /// <returns></returns>
        public SqlQuery<T> CreateQuery()
        {
            return new SqlQuery<T>(ConnectionStringSettings);
        }

        /// <summary>
        /// 获取对象列表
        /// </summary>
        /// <param name="qc"></param>
        /// <returns></returns>
        public virtual List<T> Gets(QueryCondition q)
        {
            CheckQuery(q);

            q.TableField = "*";

            BucketImpl bucket = new BucketImpl<T>().Describe();

            List<T> list = new List<T>();

            q.FireBeforeQueryEvent("Gets");

            using (IDataReader rdr = q.GetReader())
            {
                while (rdr.Read())
                {
                    var item = new T();

                    FluentBucket.As(bucket).For.EachItem.Process(delegate(BucketItem bucketItem)
                    {
                        PropertyInfo info = item.GetType().GetProperty(bucketItem.ProperyName);

                        if (info != null && info.CanWrite)
                        {
                            object o = null;
                            int index = rdr.GetOrdinal(bucketItem.Name);
                            if (index != -1)
                            {
                                o = rdr[index];
                                if (!(o is DBNull))
                                {
                                    o = TypeConvertUtil.ConvertTo(o, bucketItem.PropertyType);

                                    if (o != null)
                                        info.SetValue(item
                                            , o
                                            , null);
                                }
                            }
                        }
                    });

                    list.Add(item);
                }
            }

            return list;
        }

        /// <summary>
        /// 获取记录数
        /// </summary>
        /// <param name="qc"></param>
        /// <returns></returns>
        public virtual int Count(QueryCondition q)
        {
            CheckQuery(q);

            q.FireBeforeQueryEvent("Count");

            return q.GetRelationCount();
        }

        public virtual T Save(T obj)
        {
            Query.SubmitChanges(false);

            return obj;
        }

        public virtual List<T> GetsAll()
        {
            return (from q in Query
                    select q).ToList();
        }

        object IRepository.Gets(QueryCondition q)
        {
            return Gets(q);
        }

        private void CheckQuery(QueryCondition q)
        {
            if (q.ConnectionStringSettings == null)
                q.ConnectionStringSettings = ConnectionStringSettings;

            string tablename = Obj.GetTableName<T>();

            if (string.IsNullOrEmpty(q.ParentCacheKey))
                q.ParentCacheKey = tablename;

            if (string.IsNullOrEmpty(q.TableName))
                q.TableName = tablename;
        }

        #region IAutoStart Members

        public void Start()
        {
            CreatedEventArgs e = new CreatedEventArgs();
            e.ConnectionStringSettings = _connectionStringSettings;

            OnCreated(e);

            ConnectionStringSettings = e.ConnectionStringSettings;
        }

        #endregion
    }

    public class Repository
    {
        public static event EventHandler<CreatedEventArgs> Created;

        protected virtual void OnCreated(CreatedEventArgs e)
        {
            EventHandler<CreatedEventArgs> handler = Created;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class CreatedEventArgs : EventArgs
        {
            public static readonly new CreatedEventArgs Empty = new CreatedEventArgs();

            public ConnectionStringSettings ConnectionStringSettings { get; set; }
        }
    }
}
