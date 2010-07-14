using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiss.Linq.Fluent;
using Kiss.Utils;

namespace Kiss.Linq.Sql
{
    public class TSqlFormatProvider : IFormatProvider
    {
        protected IBucket bucket;

        #region Implementation of IFormatProvider

        /// <summary>
        /// Creates a new format provider for <see cref="Bucket"/> object.
        /// </summary>
        /// <param name="bucket"></param>
        public IFormatProvider Initialize(IBucket bucket)
        {
            this.bucket = bucket;
            return this;
        }

        public virtual string ProcessFormat()
        {
            if (FluentBucket.As(bucket).Entity.ItemsToFetch != null)
            {
                StringBuilder builder = new StringBuilder();
                string fields = "${Fields}";

                builder.AppendFormat("WITH FilteredList({0}, [RowNumber]) AS(", fields);
                builder.AppendFormat("SELECT {0} , Row_number()", fields);

                builder.Append(" OVER(${OrderBy}) ");
                builder.Append(" as [RowNumber] FROM [${Entity}] ");
                builder.Append("${Where}");
                builder.Append(")");
                builder.Append("Select * from FilteredList WHERE [Rownumber] Between (${Skip}) and (${PageLength})");

                return builder.ToString();
            }

            return "Select * from [${Entity}] ${Where} ${OrderBy}";
        }

        public string GetItemFormat()
        {
            return ProcessFormat();
        }

        public virtual string AddItemFormat()
        {
            return @"INSERT INTO [${Entity}] ( ${TobeInsertedFields}) VALUES (${TobeInsertedValues}); SELECT * FROM [${Entity}] ${AfterInsertWhere}";
        }

        public virtual string BatchAddItemFormat()
        {
            return @"INSERT INTO [${Entity}] ( ${TobeInsertedFields}) VALUES (${TobeInsertedValues});";
        }

        public string UpdateItemFormat()
        {
            return @"Update [${Entity}] SET ${UpdateItems} WHERE ${UniqueWhere}; SELECT * FROM [${Entity}] Where ${UniqueWhere}";
        }

        public string BatchUpdateItemFormat()
        {
            return @"Update [${Entity}] SET ${UpdateItems} WHERE ${UniqueWhere};";
        }

        public string RemoveItemFormat()
        {
            return @"DELETE FROM [${Entity}] WHERE ${UniqueWhere};";
        }

        public string BatchRemoveItemFormat()
        {
            return @"DELETE FROM [${Entity}] WHERE ${UniqueWhere};";
        }

        public string DefineEntity()
        {
            return FluentBucket.As(bucket).Entity.Name;
        }

        public string DefineFields()
        {
            string[] names = bucket.Items.Select(item => string.Format("[{0}]", item.Value.Name)).ToArray();
            return string.Join(",", names);
        }

        public string DefineNonUniqueFields()
        {
            List<string> list = new List<string>();

            FluentBucket.As(bucket).For.EachItem
                .Match(delegate(BucketItem item)
                {
                    return item.Unique == false;
                }).Process(delegate(BucketItem item)
                {
                    list.Add(item.Name);
                });

            return string.Join(",", list.ToArray());
        }

        public string DefineTobeInsertedFields()
        {
            List<string> list = new List<string>();

            FluentBucket.As(bucket).For.EachItem
                .Process(delegate(BucketItem item)
                {
                    if (!item.Unique || !(item.FindAttribute(typeof(PKAttribute)) as PKAttribute).AutoIncrement)
                        if (HasValue(item.Value))
                            list.Add(string.Format("[{0}]", item.Name));
                });

            return string.Join(",", list.ToArray());
        }

        public string DefineTobeInsertedValues()
        {
            StringBuilder builder = new StringBuilder();
            FluentBucket.As(bucket).For.EachItem
               .Process(delegate(BucketItem item)
               {
                   if (!item.Unique || !(item.FindAttribute(typeof(PKAttribute)) as PKAttribute).AutoIncrement)
                       if (HasValue(item.Value))
                           builder.Append(GetValue(item.Value) + ",");
               });

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public virtual string DefineSkip()
        {
            return (FluentBucket.As(bucket).Entity.ItemsToSkipFromStart + 1).ToString();
        }

        public string DefineUpdateItems()
        {
            StringBuilder builder = new StringBuilder();
            FluentBucket.As(bucket).For.EachItem
                .Process(delegate(BucketItem item)
                         {
                             if (item.IsModified)
                             {
                                 if (item.Value != null)
                                 {
                                     string value = GetValue(item.Value);
                                     builder.AppendFormat("[{0}]{1}{2},", item.Name, RelationalOperators[item.RelationType], value);
                                 }
                                 else
                                 {
                                     builder.AppendFormat("[{0}]{1}null,", item.Name, RelationalOperators[item.RelationType]);
                                 }
                             }
                         });

            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public string DefineUniqueWhere()
        {
            StringBuilder builder = new StringBuilder();
            FluentBucket.As(bucket).For.EachItem
                .Process(delegate(BucketItem item)
                {
                    if (item.Unique && item.Value != null)
                    {
                        string value = GetValue(item.Value);
                        builder.Append(item.Name + RelationalOperators[item.RelationType] + value + " AND");
                    }
                });

            if (builder.Length == 0)
                throw new LinqException(string.Format("类 {0} 未定义PKAttribute", bucket.Name));

            builder.Remove(builder.Length - 3, 3);

            return builder.ToString();
        }

        public string DefineUniqueItem()
        {
            string attr = FluentBucket.As(bucket).Entity.UniqueAttribte;

            if (StringUtil.IsNullOrEmpty(attr))
                throw new LinqException(string.Format("类 {0} 未定义PKAttribute", bucket.Name));

            return attr;
        }

        public virtual string DefineAfterInsertWhere()
        {
            string value = string.Empty;

            FluentBucket.As(bucket).For.EachItem
                .Process(delegate(BucketItem item)
            {
                if (item.Unique)
                {
                    if ((item.FindAttribute(typeof(PKAttribute)) as PKAttribute).AutoIncrement)
                        value = "WHERE [" + item.Name + "] = @@IDENTITY";
                    else if (item.Value != null)
                        value = "WHERE [" + item.Name + "] = " + GetValue(item.Value);
                }
            });

            return value;
        }

        public virtual string DefinePageLength()
        {
            int itemsToSkip = bucket.ItemsToSkip;
            int itemsToTake = bucket.ItemsToTake == null ? 100 : bucket.ItemsToTake.Value;

            return (itemsToTake + itemsToSkip).ToString();
        }

        public string DefineWhere()
        {
            StringBuilder builder = new StringBuilder();

            FluentBucket fbucket = FluentBucket.As(bucket);

            if (fbucket.IsDirty)
            {
                builder.Append("WHERE ");

                fbucket.ExpressionTree
                    .DescribeContainerAs(builder)
                    .Root((containter, operatorType) => containter.Append(" " + operatorType + " "))
                    .Begin(container => container.Append("("))
                    .EachLeaf((container, item) =>
                    {
                        if (item.RelationType == RelationType.Contains)
                        {
                            IList<string> items = new List<string>();

                            foreach (var list in item.Values)
                            {
                                items.Add(GetValue(list.Value));
                            }

                            container.AppendFormat("[{0}] IN({1})", item.Name, string.Join(",", items.ToArray()));
                        }
                        else if (item.RelationType != RelationType.NotApplicable)
                        {
                            string value = GetValue(item.Value);
                            container.AppendFormat("[{0}] {1} {2}", item.Name, RelationalOperators[item.RelationType].ToString(), value);
                        }
                    })
                    .End(container => container.Append(")"))
                    .Execute();
            }

            return builder.ToString();
        }

        public string DefineOrderBy()
        {
            StringBuilder builder = new StringBuilder();

            FluentBucket.As(bucket)
                .Entity.OrderBy.IfUsed(() => builder.Append("ORDER BY "))
                .ForEach.Process(delegate(string field, bool ascending)
            {
                builder.Append("[" + field + "] " + (ascending ? "asc" : "desc"));
                builder.Append(",");
            });

            if (builder.Length > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        /// <summary>
        /// format value
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetValue(object obj)
        {
            if (obj == null)
                obj = string.Empty;

            if (obj is int)
                return obj.ToString();

            if (obj is bool)
                return (Convert.ToBoolean(obj) ? 1 : 0).ToString();

            Type type = obj.GetType();
            if (type.IsEnum)
                return ((int)obj).ToString();

            string value;

            if (obj is DateTime)
                value = GetDateTimeValue(Convert.ToDateTime(obj));
            else
                value = Convert.ToString(obj);

            value = value.Replace("'", "''");

            value = "'" + value + "'";
            return value;
        }

        public virtual string GetDateTimeValue(DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        private static bool HasValue(object obj)
        {
            if (obj != null && obj is DateTime)
            {
                DateTime dt = (DateTime)obj;
                return dt > DateTime.MinValue && dt < DateTime.MaxValue;
            }

            return obj != null;
        }

        public IDictionary<OperatorType, string> Operators
        {
            get
            {
                if (operators == null)
                {
                    operators = new Dictionary<OperatorType, string>();
                    operators.Add(OperatorType.AND, "AND");
                    operators.Add(OperatorType.OR, "OR");
                }
                return operators;
            }
        }

        public IDictionary<RelationType, string> RelationalOperators
        {
            get
            {

                if (relationalOps == null)
                {
                    relationalOps = new Dictionary<RelationType, string>();

                    relationalOps.Add(RelationType.Equal, "=");
                    relationalOps.Add(RelationType.LessThan, "<");
                    relationalOps.Add(RelationType.GreaterThan, ">");
                    relationalOps.Add(RelationType.LessThanEqual, "<=");
                    relationalOps.Add(RelationType.NotEqual, "<>");
                    relationalOps.Add(RelationType.GreaterThanEqual, ">=");
                    relationalOps.Add(RelationType.Contains, "IN");
                    relationalOps.Add(RelationType.Like, "like");
                }
                return relationalOps;
            }
        }

        private IDictionary<OperatorType, string> operators;
        private IDictionary<RelationType, string> relationalOps;

        public string DefineString(string method)
        {
            switch (method)
            {
                case "Entity":
                    return DefineEntity();
                case "Fields":
                    return DefineFields();
                case "NonUniqueFields":
                    return DefineNonUniqueFields();
                case "TobeInsertedFields":
                    return DefineTobeInsertedFields();
                case "TobeInsertedValues":
                    return DefineTobeInsertedValues();
                case "Skip":
                    return DefineSkip();
                case "UpdateItems":
                    return DefineUpdateItems();
                case "AfterInsertWhere":
                    return DefineAfterInsertWhere();
                case "UniqueWhere":
                    return DefineUniqueWhere();
                case "UniqueItem":
                    return DefineUniqueItem();
                case "PageLength":
                    return DefinePageLength();
                case "Where":
                    return DefineWhere();
                case "OrderBy":
                    return DefineOrderBy();
                default:
                    return string.Empty;
            }
        }

        #endregion
    }
}