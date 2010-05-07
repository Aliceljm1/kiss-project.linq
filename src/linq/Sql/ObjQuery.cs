using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Kiss.Linq.Sql
{
    /// <summary>
    /// 使用linq2sql的数据访问类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjQuery<T> : SqlQuery<T> where T : Obj, new()
    {     
        #region ctor

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="connectionStringSettings"></param>
        public ObjQuery(ConnectionStringSettings connectionStringSettings)
            : base(connectionStringSettings)
        {
        }

        #endregion

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public void Delete(params int[] ids)
        {
            if (ids.Length == 0)
                return;

            foreach (int id in ids)
            {
                T obj = new T() { Id = id };

                Add(obj);
                Remove(obj);
            }

            this.SubmitChanges();
        }

        /// <summary>
        /// 创建新的对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T Create(T obj)
        {
            Add(obj);

            SubmitChanges();

            return obj;
        }

        /// <summary>
        /// 创建新的对象
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public List<T> Create(params T[] objs)
        {
            AddRange(objs);

            SubmitChanges();

            return new List<T>(objs);
        }        

        /// <summary>
        /// 获取对象列表
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<T> Gets(params int[] ids)
        {
            if (ids.Length == 0)
                return new List<T>();

            List<int> idlist = new List<int>(ids);

            List<T> list = (from obj in this
                            where new List<int>(ids).Contains(obj.Id)
                            select obj).ToList();

            list.Sort(delegate(T t1, T t2)
            {
                return idlist.IndexOf(t1.Id).CompareTo(idlist.IndexOf(t2.Id));
            });

            return list;
        }
    }
}
