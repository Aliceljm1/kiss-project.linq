using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Kiss.Linq.Fluent;
using Kiss.Linq.Sql.DataBase;
using Kiss.Query;
using Kiss.Utils;
using MySql.Data.MySqlClient;

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

        public int Count(QueryCondition q)
        {
            string where = q.WhereClause;

            string sql = string.Format("select count({1}) as count from `{0}`",
                q.TableName,
                q.TableField.IndexOfAny(new char[] { ',' }) > -1 || q.TableField.Contains(".*") ? "*" : q.TableField);

            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            logger.Debug(sql);

            object ret = ExecuteScalar(q.ConnectionString, sql);

            if (ret == null || ret is DBNull) return 0;

            return Convert.ToInt32(ret);
        }

        public IDataReader GetReader(QueryCondition condition)
        {
            string sql = combin_sql(condition);

            logger.Debug(sql);

            return ExecuteReader(condition.ConnectionString, sql);
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

        public DataTable GetDataTable(QueryCondition q)
        {
            string sql = combin_sql(q);
            logger.Debug(sql);

            DataTable dt = new DataTable();

            using (MySqlConnection conn = new MySqlConnection(q.ConnectionString))
            {
                conn.Open();

                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }

        public void Delete(QueryCondition condition)
        {
            string where = condition.WhereClause;

            string sql = string.Format("DELETE FROM {0}", condition.TableName);

            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            logger.Debug(sql);

            ExecuteNonQuery(condition.ConnectionString, sql);
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
                createBuilder.Append(GenerateDeclaration(bucketItem));
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

            return string.Format(@"CREATE TABLE `{0}` ({1})",
                fluentBucket.Entity.Name,
                createBuilder.ToString());
        }

        public string GenAddColumnSql(string tablename, string columnname, Type columntype)
        {
            return string.Format("ALTER TABLE `{0}` ADD `{1}` {2};",
                            tablename,
                            columnname,
                            GetDbType(columntype));
        }

        public string GenChangeColumnSql(string tablename, string columnname, Type columntype, string oldtype)
        {
            return string.Empty;
        }

        public string GetDbType(Type type)
        {
            switch (type.FullName)
            {
                case "System.String":
                    return "VARCHAR(200)";
                case "System.DateTime":
                    return "DATETIME";
                case "System.Int32":
                    return "int";
                case "System.Boolean":
                    return "BIT";
                case "System.Int64":
                    return "BIGINT";
                default:
                    return "VARCHAR(200)";
            }
        }

        private string GenerateDeclaration(BucketItem item)
        {
            if (item.FindAttribute(typeof(PKAttribute)) != null)
            {
                if (item.PropertyType == typeof(int))
                    return string.Format("`{0}` int NOT NULL AUTO_INCREMENT", item.Name);
                else
                    return string.Format("`{0}` VARCHAR(50) NOT NULL", item.Name);
            }
            else
                return string.Format("`{0}` {1}", item.Name, GetDbType(item.PropertyType));
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
    }
}
