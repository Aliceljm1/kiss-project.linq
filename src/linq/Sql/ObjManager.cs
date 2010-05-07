#region File Comment
//+-------------------------------------------------------------------+
//+ FileName: 	    ObjManager.cs
//+ File Created:   20090811
//+-------------------------------------------------------------------+
//+ Purpose:        业务实体类的CRUD操作，这个类的所有操作均和缓存相关
//+-------------------------------------------------------------------+
//+ History:
//+-------------------------------------------------------------------+
//+ 20090811        ZHLI Comment Created
//+-------------------------------------------------------------------+
//+ 20090827        ZHLI 增加了分页查询的相关操作，该类为数据库操作的入口
//+-------------------------------------------------------------------+
//+ 20090903        ZHLI add Query property
//+-------------------------------------------------------------------+
//+ 20090914        ZHLI make Delete method virtual
//+-------------------------------------------------------------------+
//+ 20090925        ZHLI 增加了日志操作接口
//+-------------------------------------------------------------------+
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using Kiss.Config;
using Kiss.Query;
using Kiss.Utils;
using Kiss.Linq.Fluent;
using System.Data;
using System.Reflection;

namespace Kiss.Linq.Sql
{
    /// <summary>
    /// 从NameValueCollection转换为实体对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    public delegate bool ConvertObj<T>(T obj, NameValueCollection param);

    /// <summary>
    /// 业务实体类的CRUD操作，这个类的所有操作均和缓存相关
    /// </summary>
    /// <typeparam name="T"></typeparam>
    //public class ObjManager<T> : IObjManager<T> where T : Obj, new()
    //{
    //    /// <summary>
    //    /// logger
    //    /// </summary>
    //    protected static readonly ILogger Logger = LogManager.GetLogger<T>();

    //    #region ctor

    //    /// <summary>
    //    /// ctor
    //    /// </summary>
    //    public ObjManager()
    //    {
    //    }

    //    /// <summary>
    //    /// ctor
    //    /// </summary>
    //    /// <param name="connectionStringSettings"></param>
    //    public ObjManager(ConnectionStringSettings connectionStringSettings)
    //    {
    //        ConnectionStringSettings = connectionStringSettings;
    //    }

    //    public ObjManager(string connstr_name)
    //        : this(ConfigBase.GetConnectionStringSettings(connstr_name))
    //    {
    //    }

    //    #endregion

    //    private ConnectionStringSettings _connectionStringSettings;
    //    public ConnectionStringSettings ConnectionStringSettings
    //    {
    //        get
    //        {
    //            if (_connectionStringSettings == null)
    //                throw new LinqException("connectionString is not set!");

    //            return _connectionStringSettings;
    //        }
    //        set
    //        {
    //            _connectionStringSettings = value;
    //        }
    //    }

    //    /// <summary>
    //    /// 单个对象的缓存key
    //    /// </summary>
    //    protected GetCacheKey getCacheKey = delegate(int i) { return string.Format("{0}:{1}", typeof(T).Name.ToLower(), i); };

    //    private ObjQuery<T> query;

    //    /// <summary>
    //    /// 获取linq query，其生命周期由ObjManager控制
    //    /// </summary>
    //    public ObjQuery<T> Query
    //    {
    //        get
    //        {
    //            if (query == null)
    //                query = CreateQuery();
    //            return query;
    //        }
    //    }

    //    /// <summary>
    //    /// create new linq query
    //    /// </summary>
    //    /// <returns></returns>
    //    public ObjQuery<T> CreateQuery()
    //    {
    //        return new ObjQuery<T>(ConnectionStringSettings);
    //    }

    //    object IObjManager.Get(int id)
    //    {
    //        return Get(id);
    //    }

    //    /// <summary>
    //    /// 获取单个对象
    //    /// </summary>
    //    /// <param name="id"></param>
    //    /// <returns></returns>
    //    public virtual T Get(int id)
    //    {
    //        if (Query.RetainContext)
    //            return Query.Get(id);

    //        return ObjGetter<T>.Get(id,
    //            getCacheKey,
    //            delegate(int j) { return Query.Get(j); });
    //    }

    //    object IObjManager.Gets(QueryCondition qc)
    //    {
    //        return Gets(qc);
    //    }

    //    object IObjManager.Gets(int[] ids)
    //    {
    //        return Gets(ids);
    //    }

    //    object IObjManager.GetsPaged(int pageIndex, int pageCount)
    //    {
    //        return GetsPaged(pageIndex, pageCount);
    //    }

    //    public List<T> Gets(string commaDelimitedIds)
    //    {
    //        return Gets(StringUtil.ToIntArray(StringUtil.CommaDelimitedListToStringArray(commaDelimitedIds)));
    //    }

    //    /// <summary>
    //    /// 获取对象列表
    //    /// </summary>
    //    /// <param name="ids"></param>
    //    /// <returns></returns>
    //    public virtual List<T> Gets(int[] ids)
    //    {
    //        if (Query.RetainContext)
    //            return Query.Gets(ids);

    //        return ObjGetter<T>.Gets(ids,
    //            getCacheKey,
    //            delegate(int[] j) { return Query.Gets(j); });
    //    }

    //    /// <summary>
    //    /// 获取对象列表
    //    /// </summary>
    //    /// <param name="qc"></param>
    //    /// <returns></returns>
    //    public virtual List<T> Gets(QueryCondition qc)
    //    {
    //        if (qc.ConnectionStringSettings == null)
    //            qc.ConnectionStringSettings = ConnectionStringSettings;

    //        if (string.IsNullOrEmpty(qc.ParentCacheKey))
    //            qc.ParentCacheKey = TableName;

    //        return Gets(qc.GetRelationIds().ToArray());
    //    }

    //    /// <summary>
    //    /// get all using linq( no cache )
    //    /// </summary>
    //    /// <returns></returns>
    //    public virtual List<T> GetsAll()
    //    {
    //        return (from q in Query
    //                select q).ToList();
    //    }

    //    public virtual List<T> GetsPaged(int pageIndex, int pageSize)
    //    {
    //        return (from q in Query
    //                orderby q.Id descending
    //                select q).Take(pageSize).Skip(pageSize * pageIndex).ToList();
    //    }

    //    public int Count()
    //    {
    //        return Count(new QueryCondition()
    //        {
    //            ConnectionStringSettings = ConnectionStringSettings,
    //            ParentCacheKey = TableName,
    //            TableName = TableName
    //        });
    //    }

    //    /// <summary>
    //    /// 获取记录数
    //    /// </summary>
    //    /// <param name="qc"></param>
    //    /// <returns></returns>
    //    public int Count(QueryCondition qc)
    //    {
    //        if (qc.ConnectionStringSettings == null)
    //            qc.ConnectionStringSettings = ConnectionStringSettings;

    //        if (string.IsNullOrEmpty(qc.ParentCacheKey))
    //            qc.ParentCacheKey = TableName;

    //        return qc.GetRelationCount();
    //    }

    //    /// <summary>
    //    /// 创建对象
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    /// <returns></returns>
    //    public T Create(T obj)
    //    {
    //        obj = Query.Create(obj);

    //        OnSave(obj);

    //        return obj;
    //    }

    //    /// <summary>
    //    /// 更新对象
    //    /// </summary>
    //    /// <param name="query"></param>
    //    /// <param name="obj"></param>
    //    /// <returns></returns>
    //    public T Update(SqlQuery<T> query, T obj)
    //    {
    //        query.SubmitChanges();

    //        OnSave(obj);

    //        return obj;
    //    }

    //    /// <summary>
    //    /// 删除对象
    //    /// </summary>
    //    /// <param name="ids"></param>
    //    public virtual void Delete(params int[] ids)
    //    {
    //        List<T> list = Gets(ids);

    //        Query.Delete(ids);

    //        OnDelete(list.ToArray());
    //    }

    //    /// <summary>
    //    /// 保存对象（一般用于保存表单提交过来的数据）
    //    /// </summary>
    //    public T Save(string param, ConvertObj<T> converter)
    //    {
    //        NameValueCollection nv = StringUtil.DelimitedEquation2NVCollection("&", param);

    //        return Save(nv, converter);
    //    }

    //    /// <summary>
    //    /// 保存对象（一般用于保存表单提交过来的数据）
    //    /// </summary>
    //    public T Save(NameValueCollection param, ConvertObj<T> converter)
    //    {
    //        Query.RetainContext = true;

    //        int id = StringUtil.GetInt(param["id"], 0);

    //        T obj;

    //        if (id == 0)
    //        {
    //            obj = new T();
    //            Query.Add(obj);
    //        }
    //        else
    //        {
    //            // always get from db here. stay in query context
    //            obj = Query.Get(id);

    //            if (obj == null)
    //                throw new ArgumentException(string.Format("{0} object not exist. Id:{1}", typeof(T).Name, id));
    //        }

    //        if (!converter(obj, param))
    //            return null;

    //        Query.SubmitChanges();

    //        OnSave(obj);

    //        return obj;
    //    }

    //    /// <summary>
    //    /// 保存对象时触发
    //    /// </summary>
    //    /// <param name="obj"></param>
    //    protected virtual void OnSave(T obj)
    //    {
    //        if (obj == null) return;
    //        JCache.Insert(getCacheKey(obj.Id), obj);
    //    }

    //    /// <summary>
    //    /// 重载此方法用于处理删除对象后的一些操作，etc：清空缓存
    //    /// </summary>
    //    /// <param name="objs"></param>
    //    protected virtual void OnDelete(params T[] objs)
    //    {
    //        foreach (T obj in objs)
    //        {
    //            JCache.Remove(getCacheKey(obj.Id));
    //        }
    //    }

    //    public void Delete(QueryCondition q)
    //    {
    //        if (q.ConnectionStringSettings == null)
    //            q.ConnectionStringSettings = ConnectionStringSettings;

    //        q.Delete();

    //        OnDelete();
    //    }

    //    public string TableName
    //    {
    //        get
    //        {
    //            return Obj.GetTableName(typeof(T));
    //        }
    //    }
    //}

    public class Repository<T, t> : Repository<T>, IRepository<T, t>, IRepository<T>
        where T : Obj<t>, new()
        where t : IEquatable<t>
    {
        public T Get(t id)
        {
            return (from obj in Query
                    where obj.Id.Equals(id)
                    select obj).FirstOrDefault();
        }

        public List<T> Gets(t[] ids)
        {
            throw new NotImplementedException();
        }
    }

    public class Repository<T> : IRepository<T> where T : class, IQueryObject, new()
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

        private SqlQuery<T> query;

        /// <summary>
        /// 获取linq query，其生命周期由ObjManager控制
        /// </summary>
        public SqlQuery<T> Query
        {
            get
            {
                if (query == null)
                    query = CreateQuery();
                return query;
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
        public virtual List<T> Gets(QueryCondition qc)
        {
            if (qc.ConnectionStringSettings == null)
                qc.ConnectionStringSettings = ConnectionStringSettings;

            if (string.IsNullOrEmpty(qc.ParentCacheKey))
                qc.ParentCacheKey = TableName;

            qc.TableField = "*";

            BucketImpl bucket = new BucketImpl<T>().Describe();

            List<T> list = new List<T>();

            using (IDataReader rdr = qc.GetReader())
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
                                    o = TypeConvertUtil.ConvertTo(o, bucketItem.PropertyType);

                                if (o != null)
                                    info.SetValue(item
                                        , o
                                        , null);
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
        public virtual int Count(QueryCondition qc)
        {
            if (qc.ConnectionStringSettings == null)
                qc.ConnectionStringSettings = ConnectionStringSettings;

            if (string.IsNullOrEmpty(qc.ParentCacheKey))
                qc.ParentCacheKey = TableName;

            return qc.GetRelationCount();
        }

        private string TableName
        {
            get
            {
                return Obj.GetTableName(typeof(T));
            }
        }

        public T Save(T obj)
        {
            Query.SubmitChanges();

            return obj;
        }

        object IRepository.Gets(QueryCondition q)
        {
            return Gets(q);
        }
    }
}
