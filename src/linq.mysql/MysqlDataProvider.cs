﻿using Kiss.Linq.Fluent;
using Kiss.Linq.Sql.DataBase;
using Kiss.Query;
using Kiss.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using Kiss.Linq;
using System.Reflection;
using System.Text;

namespace Kiss.Linq.Sql.Mysql
{
    [DbProvider(ProviderName = "System.Data.Mysql")]
    public class MysqlDataProvider : IDataProvider, Kiss.Query.IQuery, IDDL
    {
        public int ExecuteNonQuery(string connstring, string sql)
        {
            int ret = 0;

            using (DbConnection conn = new MySqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = conn.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                ret = command.ExecuteNonQuery();

                conn.Close();
            }

            return ret;
        }

        public int ExecuteNonQuery(IDbTransaction tran, string sql)
        {
            IDbCommand command = tran.Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Transaction = tran;

            return command.ExecuteNonQuery();
        }

        public object ExecuteScalar(string connstring, string sql)
        {
            object ret = 0;

            using (DbConnection conn = new MySqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = conn.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                ret = command.ExecuteScalar();

                conn.Close();
            }

            return ret;
        }

        public object ExecuteScalar(IDbTransaction tran, string sql)
        {
            IDbCommand command = tran.Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Transaction = tran;

            return command.ExecuteScalar();
        }

        public IDataReader ExecuteReader(string connstring, string sql)
        {
            DbConnection conn = new MySqlConnection(connstring);
            conn.Open();

            DbCommand command = conn.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteReader(IDbTransaction tran, string sql)
        {
            IDbCommand command = tran.Connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Transaction = tran;

            return command.ExecuteReader();
        }

        public DataTable ExecuteDataTable(string connstring, string sql)
        {
            DataTable dt = new DataTable();

            using (DbConnection conn = new MySqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = new MySqlCommand(sql, (MySqlConnection)conn);
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                MySqlDataAdapter da = new MySqlDataAdapter((MySqlCommand)command);

                da.Fill(dt);
            }

            return dt;
        }

        public DataTable ExecuteDataTable(IDbTransaction tran, string sql)
        {
            DataTable dt = new DataTable();

            IDbCommand command = new MySqlCommand(sql, (MySqlConnection)tran.Connection);
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Transaction = tran;

            MySqlDataAdapter da = new MySqlDataAdapter((MySqlCommand)command);

            da.Fill(dt);

            return dt;
        }

        public IFormatProvider GetFormatProvider(string connstring) { return new MysqlFormatProvider(); }

        private static readonly ILogger logger = LogManager.GetLogger(typeof(MysqlDataProvider));

        public List<T> GetRelationIds<T>(QueryCondition condition)
        {
            List<T> li = new List<T>();

            using (IDataReader rdr = GetReader(condition))
            {
                while (rdr.Read())
                {
                    li.Add((T)Convert.ChangeType(rdr[0], typeof(T)));
                }
            }

            return li;
        }

        public int Count(QueryCondition qc)
        {
            string where = qc.WhereClause;

            string sql = string.Format("select count({1}) as count from {0}",
                qc.TableName,
                qc.TableField.IndexOfAny(new char[] { ',' }) > -1 || qc.TableField.Contains(".*") ? "*" : qc.TableField);

            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            logger.Debug(sql);

            object ret = 0;

            using (MySqlConnection conn = new MySqlConnection(qc.ConnectionString))
            {
                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                if (qc.Parameters.Count > 0)
                {
                    foreach (var item in qc.Parameters)
                    {
                        cmd.Parameters.AddWithValue(item.Key, item.Value);
                    }
                }

                ret = cmd.ExecuteScalar();

                conn.Close();
            }

            if (ret == null || ret is DBNull) return 0;

            return Convert.ToInt32(ret);
        }

        public IDataReader GetReader(QueryCondition qc)
        {
            string sql = combin_sql(qc);

            logger.Debug(sql);

            MySqlConnection conn = new MySqlConnection(qc.ConnectionString);
            conn.Open();

            MySqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;

            if (qc.Parameters.Count > 0)
            {
                foreach (var item in qc.Parameters)
                {
                    cmd.Parameters.AddWithValue(item.Key, item.Value);
                }
            }

            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDbTransaction BeginTransaction(string connectionstring, IsolationLevel isolationLevel)
        {
            MySqlConnection connection = new MySqlConnection(connectionstring);
            connection.Open();

            return connection.BeginTransaction(isolationLevel);
        }

        private static string combin_sql(QueryCondition condition)
        {
            string where = condition.WhereClause;

            string sql = string.Format("select {0} from {1}", condition.TableField, condition.TableName);
            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            if (StringUtil.HasText(condition.OrderByClause))
                sql += string.Format(" order by {0}", condition.OrderByClause);

            if (condition.Paging)
            {
                sql += string.Format(" limit {0},{1}", condition.PageSize * condition.PageIndex, condition.PageSize);
            }
            else if (condition.TotalCount > 0)
            {
                sql += string.Format(" limit {0}", condition.TotalCount);
            }
            return sql;
        }

        public DataTable GetDataTable(QueryCondition qc)
        {
            string sql = combin_sql(qc);
            logger.Debug(sql);

            DataTable dt = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(qc.ConnectionString))
            {
                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                if (qc.Parameters.Count > 0)
                {
                    foreach (var item in qc.Parameters)
                    {
                        cmd.Parameters.AddWithValue(item.Key, item.Value);
                    }
                }

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }

        #region IDDL Members

        public void Init(string conn_string)
        {
        }

        public void Fill(Database db)
        {
            using (MySqlConnection conn = new MySqlConnection(db.Connectionstring))
            {
                conn.Open();

                DataTable dt = conn.GetSchema("Columns");

                foreach (DataRow row in dt.Rows)
                {
                    string type = row["DATA_TYPE"].ToString();
                    if (StringUtil.IsNullOrEmpty(type))
                        continue;

                    string tablename = row["TABLE_NAME"].ToString();

                    Table tb = db.FindTable(tablename);
                    if (tb == null)
                    {
                        tb = new Table();
                        tb.Name = tablename;

                        db.Tables.Add(tb);
                    }

                    tb.Columns.Add(new Column() { Name = row["COLUMN_NAME"].ToString(), Type = type });
                }
            }
        }

        public void Execute(Database db, string sql)
        {
            ExecuteNonQuery(db.Connectionstring, sql);
        }

        public string GenAddTableSql(IBucket bucket)
        {
            StringBuilder createBuilder = new StringBuilder();

            FluentBucket fluentBucket = FluentBucket.As(bucket);

            fluentBucket.For.EachItem.Process(delegate(BucketItem bucketItem)
            {
                createBuilder.Append(GenerateColumnDeclaration(bucketItem));
                createBuilder.Append(",\n");
            });

            fluentBucket.For.EachItem.Match(delegate(BucketItem bucketItem)
            {
                return bucketItem.Unique;
            }).Process(delegate(BucketItem bucketItem)
            {
                createBuilder.AppendFormat("CONSTRAINT `PK_{0}` PRIMARY KEY (`{1}`)", bucket.Name, bucketItem.Name);
                createBuilder.Append(",\n");
            });

            createBuilder.Remove(createBuilder.Length - 2, 2);

            return string.Format(@"CREATE TABLE `{0}` ({1});",
                fluentBucket.Entity.Name,
                createBuilder.ToString());
        }

        public string GenAlterTableSql(IBucket bucket, BucketItem item)
        {
            return string.Format("ALTER TABLE `{0}` ADD {1};",
                                  bucket.Name,
                                  GenerateColumnDeclaration(item));
        }

        public string GenerateColumnDeclaration(BucketItem item)
        {
            bool isPk = item.FindAttribute(typeof(PKAttribute)) != null;

            int maxLength = 500;
            if (isPk)
            {
                maxLength = 50;
            }
            else if (item.PropertyType.FullName == "System.String")
            {
                Validation.LengthAttribute lengthAttr = item.FindAttribute(typeof(Validation.LengthAttribute)) as Validation.LengthAttribute;

                if (lengthAttr != null)
                    maxLength = (int)lengthAttr.MaxLength;
            }

            Validation.NotNullAttribute notnullattr = item.FindAttribute(typeof(Validation.NotNullAttribute)) as Validation.NotNullAttribute;

            StringBuilder column = new StringBuilder();
            column.AppendFormat("`{0}` ", item.Name);

            Type propertyType = item.PropertyType;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                NullableConverter nc = new NullableConverter(propertyType);
                propertyType = nc.UnderlyingType;
            }

            switch (propertyType.FullName)
            {
                case "System.DateTime":
                    column.Append("DATETIME");
                    break;
                case "System.Int32":
                    column.Append("INT");
                    break;
                case "System.Boolean":
                    column.Append("BIT");
                    break;
                case "System.Int64":
                    column.Append("BIGINT");
                    break;
                case "System.Decimal":
                    column.Append("DECIMAL(10,1)");
                    break;
                case "System.String":
                    if (maxLength > 4000)
                        column.Append("TEXT");
                    else
                        column.AppendFormat("NVARCHAR({0})", maxLength);
                    break;
                default:
                    column.AppendFormat("NVARCHAR({0})", maxLength);
                    break;
            }

            if (isPk)
                column.AppendFormat(" NOT NULL {0}",
                    item.PropertyType == typeof(int) ? "AUTO_INCREMENT" : string.Empty);
            else if (notnullattr != null)
            {
                MysqlFormatProvider fp = new MysqlFormatProvider();
                column.AppendFormat(" NOT NULL DEFAULT {0}",
                    fp.GetValue(notnullattr.DefaultValue ?? (item.PropertyType.IsValueType ? Activator.CreateInstance(item.PropertyType) : null)));
            }

            return column.ToString();
        }

        #endregion

        public void BulkCopy<T>(string connstring, Bucket bucket, List<QueryObject<T>> list) where T : IQueryObject, new()
        {
            if (list.Count == 0) return;

            IFormatProvider fp = GetFormatProvider(connstring);

            string sql_pre = SqlQuery<T>.Translate(list[0].FillBucket(bucket), FormatMethod.BatchAdd, fp);

            List<string> datas = new List<string>();

            foreach (var item in list)
            {
                datas.Add(SqlQuery<T>.Translate(item.FillBucket(bucket), FormatMethod.BatchAddValues, fp));
            }

            int ps = 10000;

            int pc = (int)Math.Ceiling(datas.Count / (ps * 1.0));
            for (int i = 0; i < pc; i++)
            {
                ExecuteNonQuery(connstring, sql_pre + datas.GetRange(i * ps, Math.Min(datas.Count - i * ps, ps)).Join(StringUtil.Comma));
            }
        }

        public void SaveDataTable(string connstring, DataTable dt)
        {
            if (string.IsNullOrEmpty(connstring) || dt == null || string.IsNullOrEmpty(dt.TableName))
                throw new ArgumentNullException();

            if (dt.Columns.Count == 0 || dt.Rows.Count == 0) return;

            Type type = Type.GetType(dt.TableName);

            if (type == null)
                throw new ArgumentException(string.Format("type {0} is not found!", dt.TableName));

            IFormatProvider fp = GetFormatProvider(connstring);

            IBucket bucket = new BucketImpl(type).Describe();

            fp.Initialize(bucket);

            Type querytype = typeof(SqlQuery<>).MakeGenericType(type);

            MethodInfo mi = querytype.GetMethod("Translate");

            string sql_pre = mi.Invoke(null, new object[] { bucket, FormatMethod.BatchAdd, fp }) as string;

            List<string> datas = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                datas.Add(string.Format("({0})", fp.DefineBatchTobeInsertedValues(row)));
            }

            int ps = 10000;

            int pc = (int)Math.Ceiling(datas.Count / (ps * 1.0));
            for (int i = 0; i < pc; i++)
            {
                ExecuteNonQuery(connstring, sql_pre + datas.GetRange(i * ps, Math.Min(datas.Count - i * ps, ps)).Join(StringUtil.Comma));
            }
        }
    }
}
