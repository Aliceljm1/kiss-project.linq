using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;
using Kiss.Linq.Fluent;
using Kiss.Query;
using Kiss.Utils;

namespace Kiss.Linq.Sql.DataBase
{
    [DbProvider(ProviderName = "System.Data.SQLite")]
    public class SqliteDataProvider : IDataProvider, Kiss.Query.IQuery, IDDL
    {
        public int ExecuteNonQuery(string connstring, CommandType cmdType, string sql)
        {
            int ret = 0;

            using (DbConnection conn = new SQLiteConnection(connstring))
            {
                conn.Open();

                DbCommand command = conn.CreateCommand();
                command.CommandType = cmdType;
                command.CommandText = sql;

                ret = command.ExecuteNonQuery();

                conn.Close();
            }

            return ret;
        }

        public int ExecuteNonQuery(IDbTransaction tran, CommandType cmdType, string sql)
        {
            IDbCommand command = tran.Connection.CreateCommand();
            command.CommandType = cmdType;
            command.CommandText = sql;
            command.Transaction = tran;

            return command.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader(string connstring, CommandType cmdType, string sql)
        {
            DbConnection conn = new SQLiteConnection(connstring);
            conn.Open();

            DbCommand command = conn.CreateCommand();
            command.CommandType = cmdType;
            command.CommandText = sql;

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteReader(IDbTransaction tran, CommandType cmdType, string sql)
        {
            IDbCommand command = tran.Connection.CreateCommand();
            command.CommandType = cmdType;
            command.CommandText = sql;
            command.Transaction = tran;

            return command.ExecuteReader();
        }

        public IFormatProvider FormatProvider { get { return new SqliteFormatProvider(); } }

        private static readonly ILogger logger = LogManager.GetLogger(typeof(SqliteDataProvider));

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

            using (DbConnection conn = new SQLiteConnection(q.ConnectionString))
            {
                conn.Open();

                string sql = string.Format("select count({1}) as count from {0}",
                    q.TableName,
                    q.TableField.IndexOfAny(new char[] { ',' }) > -1 || q.TableField.Contains(".*") ? "*" : q.TableField);

                if (StringUtil.HasText(where))
                    sql += string.Format(" {0}", where);

                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                logger.Debug(sql);

                object obj = cmd.ExecuteScalar();

                if (obj == null || obj is DBNull)
                    return 0;

                return Convert.ToInt32(obj);
            }
        }

        public IDataReader GetReader(QueryCondition condition)
        {
            string sql = combin_sql(condition);

            logger.Debug(sql);

            return ExecuteReader(condition.ConnectionString, CommandType.Text, sql);
        }

        public IDbTransaction BeginTransaction(string connectionstring)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionstring);
            connection.Open();

            return connection.BeginTransaction();
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

            using (SQLiteConnection conn = new SQLiteConnection(q.ConnectionString))
            {
                conn.Open();

                SQLiteCommand cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;

                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
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

            ExecuteNonQuery(condition.ConnectionString, CommandType.Text, sql);
        }

        #region IDDL Members

        public void Init(string conn_string)
        {
        }

        public void Fill(Database db)
        {
            using (SQLiteConnection conn = new SQLiteConnection(db.Connectionstring))
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
            ExecuteNonQuery(db.Connectionstring, CommandType.Text, sql);
        }

        public string GenAddTableSql(IBucket bucket)
        {
            StringBuilder createBuilder = new StringBuilder();

            FluentBucket fluentBucket = FluentBucket.As(bucket);

            fluentBucket.For.EachItem.Match(delegate(BucketItem bucketItem)
            {
                return bucketItem.Unique;
            }).Process(delegate(BucketItem bucketItem)
            {
                createBuilder.Append(GenerateDeclaration(bucketItem));
                createBuilder.Append(",\n");
            });

            fluentBucket.For.EachItem.Match(delegate(BucketItem bucketItem)
            {
                return !bucketItem.Unique;
            }).Process(delegate(BucketItem bucketItem)
            {
                createBuilder.Append(GenerateDeclaration(bucketItem));
                createBuilder.Append(",\n");
            });

            createBuilder.Remove(createBuilder.Length - 2, 2);

            // Create script if necessary
            return ScriptProcessor.CreateTableScript(DbMode.sqlite,
                ScriptProcessor.ActionKey.ENTITY,
                fluentBucket.Entity.Name,
                ScriptProcessor.ActionKey.PARAMS,
                createBuilder.ToString());
        }

        public string GenAddColumnSql(string tablename, string columnname, Type columntype)
        {
            return string.Format("ALTER TABLE {0} ADD [{1}] {2};",
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
                    return "nvarchar";
                case "System.DateTime":
                    return "DATETIME";
                case "System.Int32":
                    return "int";
                case "System.Boolean":
                    return "BOOL";
                case "System.Int64":
                    return "BIGINT";
                default:
                    return "nvarchar";
            }
        }

        private string GenerateDeclaration(BucketItem item)
        {
            if (item.FindAttribute(typeof(PKAttribute)) != null)
            {
                if (item.PropertyType == typeof(int))
                    return string.Format("[{0}] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT", item.Name);
                else
                    return string.Format("[{0}] NVARCHAR NOT NULL PRIMARY KEY", item.Name);
            }
            else
                return string.Format("[{0}] {1}", item.Name, GetDbType(item.PropertyType));
        }

        #endregion
    }
}
