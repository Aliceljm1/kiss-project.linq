#region File Comment
//+-------------------------------------------------------------------+
//+ File Created:   2009-05-14
//+-------------------------------------------------------------------+
//+ History:
//+-------------------------------------------------------------------+
//+ 2009-05-14		zhli fix a bug in property IsNewlyAdded 
//+                      remove try{}catch{} in property IsAltered                         
//+-------------------------------------------------------------------+
//+ 2009-09-03		zhli FillProperties method ingone property with Ingore attribute                       
//+-------------------------------------------------------------------+
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kiss.Linq
{
    public class QueryObject
    {
        internal object ReferringObject { get; set; }
    }

    public sealed class QueryObject<T> : QueryObject, IVersionItem, IQueryObjectImpl where T : IQueryObject
    {
        public QueryObject(T baseObject)
        {
            ReferringObject = baseObject;
        }

        #region Tracking properties

        /// <summary>
        /// determines if an item is removed from collection.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// deternmines if the object is altered , thus call UpdateItemFormat.
        /// </summary>
        public bool IsAltered
        {
            get
            {
                PropertyInfo[] infos = ReferringObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

                object item = (this as IVersionItem).Item;
                if (item == null)
                    return false;

                foreach (PropertyInfo info in infos)
                {
                    object[] attr = info.GetCustomAttributes(typeof(IgnoreAttribute), true);
                    if (attr.Length > 0 || !info.CanWrite)
                        continue;

                    object source = info.GetValue(ReferringObject, null);

                    PropertyInfo targetInfo = item.GetType().GetProperty(info.Name);

                    object target = targetInfo.GetValue(item, null);

                    if (!object.Equals(source, target))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// determines if an item is newly added in the collection.
        /// </summary>
        public bool IsNewlyAdded
        {
            get
            {
                /// Loads the uniqueKey mapping from extention method.                
                IDictionary<string, object> uniqueDefaultValues = ReferringObject.GetUniqueItemDefaultDetail();

                foreach (string key in uniqueDefaultValues.Keys)
                {
                    PropertyInfo info = typeof(T).GetProperty(key);
                    /// create a hollow anonymous type and cast with result.
                    var item = QueryExtension.Cast(uniqueDefaultValues[key], new { Index = 0, Value = default(object) });

                    object obj = info.GetValue(ReferringObject, null);

                    if (obj != null)
                    {
                        /// the property is not nullable, check for the default value.
                        isNew = obj.Equals(item.Value);
                    }
                }
                return isNew;
            }
        }

        #endregion

        public new T ReferringObject
        {
            get { return (T)base.ReferringObject; }
            set
            {
                base.ReferringObject = value;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            mirrorObject = default(T);
        }

        #endregion

        #region IVersionItem Members

        /// <summary>
        /// updates the cached object with update object
        /// </summary>
        void IVersionItem.Commit()
        {
            //First we create an instance of this specific type.
            mirrorObject = Activator.CreateInstance<T>();
            FillProperties(ReferringObject, mirrorObject);
        }

        /// <summary>
        /// converts the current object to cachedObject.
        /// </summary>
        void IVersionItem.Revert()
        {
            FillProperties(mirrorObject, ReferringObject);
        }

        private static void FillProperties(object sourceObject, object targetObject)
        {
            Type sourceType = sourceObject.GetType();

            PropertyInfo[] infos = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in infos)
            {
                if (info.GetCustomAttributes(typeof(IgnoreAttribute), true).Length > 0)
                    continue;

                if (info.CanRead && info.CanWrite)
                {
                    info.SetValue(targetObject, info.GetValue(sourceObject, null), null);
                }
            }
        }

        object IVersionItem.Item
        {
            get
            {
                return mirrorObject;
            }
        }

        private T mirrorObject;

        #endregion

        #region Bucket Fill ups
        /// <summary>
        /// Takes bucket reference and fills it up with new values.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        public Bucket FillBucket(Bucket bucket)
        {
            PropertyInfo[] infos = ReferringObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo info in infos)
            {
                object[] arg = info.GetCustomAttributes(typeof(IgnoreAttribute), true);

                if (arg.Length > 0 || !info.CanRead)
                    continue;

                object value = info.GetValue(ReferringObject, null);
                object oldValue = (mirrorObject != null) ? info.GetValue(mirrorObject, null) : null;

                bucket.Items[info.Name].IsModified = !object.Equals(value, oldValue);

                bucket.Items[info.Name].Values.Clear();

                bucket.Items[info.Name].Values.Add(new BucketItem.QueryCondition(value, RelationType.Equal));
            }

            return bucket;
        }

        /// <summary>
        /// Fill value for a property name.
        /// </summary>
        /// <param name="name">Name of the property, accepts original property or Modified by OriginalFieldNameAttribute</param>
        /// <param name="value">the value of the property , retrived from property get accessor.</param>
        public void FillProperty(string name, object value)
        {
            PropertyInfo info = ReferringObject.GetType().GetProperty(name);

            if (info.CanWrite)
            {
                info.SetValue(ReferringObject
                    , Convert.ChangeType(value, info.PropertyType)
                    , null);
            }
        }

        public void FillObject(Bucket source)
        {
            foreach (string property in source.Items.Keys)
            {
                BucketItem item = source.Items[property];
                /// first make sure it is not turned off by user.
                if (item.Visible)
                {
                    // people can set only once condition from Query<T>.ProcessFormat
                    // check if the propety has some value, if so then proceed.
                    if (item.Values.Count > 0 && item.Values[0].Changed)
                    {
                        // change the item.
                        FillProperty(property, item.Value);
                    }
                }
            }
        }
        #endregion

        private bool isNew = true;
    }
}
