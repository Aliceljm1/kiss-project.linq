using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Kiss.Linq.Fluent;
using Kiss.Query;
using Kiss.Utils;

namespace Kiss.Linq.Sql.DataBase
{
    [DbProvider(ProviderName = "System.Data.SqlClient")]
    public class SqlDataProvider : IDataProvider, Kiss.Query.IQuery, IDDL
    {
        public int ExecuteNonQuery(string connstring, string sql)
        {
            int ret = 0;

            using (DbConnection conn = new SqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
                command.CommandType = CommandType.Text;

                ret = command.ExecuteNonQuery();

                conn.Close();
            }

            return ret;
        }

        public int ExecuteNonQuery(IDbTransaction tran, string sql)
        {
            IDbCommand command = new SqlCommand(sql, (SqlConnection)tran.Connection);
            command.CommandType = CommandType.Text;
            command.Transaction = tran;

            return command.ExecuteNonQuery();
        }

        public object ExecuteScalar(string connstring, string sql)
        {
            object ret;

            using (DbConnection conn = new SqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
                command.CommandType = CommandType.Text;

                ret = command.ExecuteScalar();

                conn.Close();
            }
            return ret;
        }

        public object ExecuteScalar(IDbTransaction tran, string sql)
        {
            IDbCommand command = new SqlCommand(sql, (SqlConnection)tran.Connection);
            command.CommandType = CommandType.Text;
            command.Transaction = tran;

            return command.ExecuteScalar();
        }

        public IDataReader ExecuteReader(string connstring, string sql)
        {
            DbConnection conn = new SqlConnection(connstring);
            conn.Open();

            DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
            command.CommandType = CommandType.Text;

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteReader(IDbTransaction tran, string sql)
        {
            IDbCommand command = new SqlCommand(sql, (SqlConnection)tran.Connection);
            command.CommandType = CommandType.Text;
            command.Transaction = tran;

            return command.ExecuteReader();
        }

        public DataTable ExecuteDataTable(string connstring, string sql)
        {
            DataTable dt = new DataTable();

            using (DbConnection conn = new SqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
                command.CommandType = CommandType.Text;
                command.CommandText = sql;

                SqlDataAdapter da = new SqlDataAdapter((SqlCommand)command);

                da.Fill(dt);
            }

            return dt;
        }

        public DataTable ExecuteDataTable(IDbTransaction tran, string sql)
        {
            DataTable dt = new DataTable();

            IDbCommand command = new SqlCommand(sql, (SqlConnection)tran.Connection);
            command.CommandType = CommandType.Text;
            command.CommandText = sql;
            command.Transaction = tran;

            SqlDataAdapter da = new SqlDataAdapter((SqlCommand)command);

            da.Fill(dt);

            return dt;
        }

        public IFormatProvider GetFormatProvider(string connstring)
        {
            if (!sqlserver2000.ContainsKey(connstring))
            {
                lock (sqlserver2000)
                {
                    if (!sqlserver2000.ContainsKey(connstring))
                        sqlserver2000.Add(connstring, SqlHelper.GetVersion(connstring) == SqlHelper.Version.SQLServer2000);
                }
            }

            if (sqlserver2000[connstring])
            {
                return new TSql2000FormatProvider();
            }
            else
            {
                return new TSqlFormatProvider();
            }
        }

        #region Iquery

        private static readonly ILogger logger = LogManager.GetLogger(typeof(SqlDataProvider));

        /// <summary>
        /// 根据查询条件获取对象的主键列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<T> GetRelationIds<T>(QueryCondition query)
        {
            List<T> li = new List<T>();

            using (IDataReader rdr = GetReader(query))
            {
                int index = query.Paging ? 1 : 0;
                while (rdr.Read())
                    li.Add((T)rdr[index]);
            }

            return li;
        }

        /// <summary>
        /// 根据查询条件获取记录总数
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public int Count(QueryCondition query)
        {
            string where = query.WhereClause;

            string sql = string.Format("Select ISNULL(COUNT({1}),0) FROM {0}",
                query.TableName,
                query.TableField.IndexOfAny(new char[] { ',' }) > -1 || query.TableField.Contains(".*") ? "*" : query.TableField);

            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            logger.Debug(sql);

            object ret = ExecuteScalar(query.ConnectionString, sql);

            if (ret == null || ret is DBNull) return 0;

            return Convert.ToInt32(ret);
        }

        /// <summary>
        /// get IDataReader from query condition
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IDataReader GetReader(QueryCondition query)
        {
            string sql = combin_sql(query);

            logger.Debug(sql);

            return ExecuteReader(query.ConnectionString,
                    sql);
        }

        public IDbTransaction BeginTransaction(string connectionstring, IsolationLevel isolationLevel)
        {
            SqlConnection connection = new SqlConnection(connectionstring);
            connection.Open();

            return connection.BeginTransaction(isolationLevel);
        }

        private Dictionary<string, bool> sqlserver2000 = new Dictionary<string, bool>();

        private string combin_sql(QueryCondition query)
        {
            string where = query.WhereClause;

            string orderby = string.Empty;
            if (StringUtil.HasText(query.OrderByClause))
                orderby = string.Format("ORDER BY {0}", query.OrderByClause);

            string sql = string.Empty;

            if (query.Paging)
            {
                if (!sqlserver2000.ContainsKey(query.ConnectionString))
                {
                    lock (sqlserver2000)
                    {
                        if (!sqlserver2000.ContainsKey(query.ConnectionString))
                            sqlserver2000.Add(query.ConnectionString, SqlHelper.GetVersion(query.ConnectionString) == SqlHelper.Version.SQLServer2000);
                    }
                }

                if (sqlserver2000[query.ConnectionString])
                {
                    string join = string.Empty;

                    var index = where.IndexOf("where", StringComparison.InvariantCultureIgnoreCase);

                    if (index != -1)
                        join = where.Substring(0, index);

                    sql = string.Format("if exists(select 1 from tempdb..sysobjects where type= 'u' and id = object_id(N'tempdb..#PageIndex')) drop table #PageIndex; CREATE TABLE #PageIndex (IndexId int IDENTITY (1, 1) NOT NULL,TID nvarchar(100) );INSERT INTO #PageIndex (TID) SELECT CAST({1}.{6} AS nvarchar(100)) FROM {1} {5} {2} SELECT {0} FROM {1} {7}, #PageIndex PageIndex WHERE {1}.{6} = PageIndex.TID AND PageIndex.IndexID > {3} AND PageIndex.IndexID < {4} ORDER BY PageIndex.IndexID; if exists(select 1 from tempdb..sysobjects where type= 'u' and name like '#PageIndex%') drop table #PageIndex",
                        query.TableField,
                        query.TableName,
                        orderby,
                        query.PageSize * query.PageIndex,
                        query.PageSize * (query.PageIndex + 1) + 1,
                        where,
                        string.IsNullOrEmpty(query["pk"]) ? "Id" : query["pk"],
                        join);
                }
                else
                {
                    int startIndex = query.PageSize * query.PageIndex + 1;
                    sql = string.Format("WITH tempTab AS ( SELECT {1}, ROW_NUMBER() OVER (Order By {0}) AS Row from {2} {3})  Select * FROM tempTab Where Row between {4} and {5}",
                        StringUtil.HasText(query.OrderByClause) ? query.OrderByClause : "rand()",
                        query.TableField,
                        query.TableName,
                        where,
                        startIndex,
                        startIndex + query.PageSize - 1);
                }
            }
            else
            {
                if (query.TotalCount == 0)
                    sql = string.Format("SELECT {0} FROM {1} {2} {3}",
                        query.TableField,
                        query.TableName,
                        where,
                        orderby);
                else
                    sql = string.Format("SELECT TOP {4} {0} FROM {1} {2} {3}",
                        query.TableField,
                        query.TableName,
                        where,
                        orderby,
                        query.TotalCount);
            }
            return sql;
        }

        public DataTable GetDataTable(QueryCondition q)
        {
            string sql = combin_sql(q);

            logger.Debug(sql);

            return ExecuteDataTable(q.ConnectionString, sql);
        }

        public void Delete(QueryCondition query)
        {
            string where = query.WhereClause;

            string sql = string.Format("DELETE FROM [{0}]",
                query.TableName);

            if (StringUtil.HasText(where))
                sql += string.Format(" {0}", where);

            logger.Debug(sql);

            ExecuteNonQuery(query.ConnectionString, sql);
        }

        #endregion

        #region ddl

        private int colNameIndex = -1;
        private int colTypeIndex = -1;
        private int colDescIndex = -1;

        private int TableNameIndex = -1;

        private void InitTableIndex(IDataRecord reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (TableNameIndex == -1)
            {
                TableNameIndex = reader.GetOrdinal("TableName");
            }
        }

        private void InitColIndex(IDataRecord reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (colNameIndex == -1)
            {
                colNameIndex = reader.GetOrdinal("Name");

                colTypeIndex = reader.GetOrdinal("Type");

                colDescIndex = reader.GetOrdinal("value");
            }
        }

        private void FillColumn(Table table, SqlDataReader reader)
        {
            Column col = new Column();

            col.Name = (string)reader[colNameIndex];

            col.Type = (string)reader[colTypeIndex];

            object o = reader[colDescIndex];
            if (o != null && !(o is DBNull))
                col.Desc = o.ToString();

            table.Columns.Add(col);
        }

        public void Init(string conn_string)
        {
            colNameIndex = -1;
            colTypeIndex = -1;
            colDescIndex = -1;
            TableNameIndex = -1;
            // create database if not exist
            SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder(conn_string);

            string database_name = cb.InitialCatalog;
            cb.InitialCatalog = "master";
            using (SqlConnection conn = new SqlConnection(cb.ConnectionString))
            {
                string sql = string.Empty;

                if (SqlHelper.GetVersion(cb.ConnectionString) == SqlHelper.Version.SQLServer2000)
                    sql = string.Format("IF NOT EXISTS (SELECT name FROM sysdatabases WHERE name = N'{0}') CREATE DATABASE [{0}]", database_name);
                else
                    sql = string.Format("IF not EXISTS (SELECT name FROM sys.databases WHERE name = N'{0}') CREATE DATABASE [{0}]", database_name);

                SqlCommand cmd = new SqlCommand(sql, conn);
                conn.Open();

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void Fill(Database database)
        {
            string lastTableName = string.Empty;
            bool isTable = true;
            Table item = null;

            using (SqlConnection conn = new SqlConnection(database.Connectionstring))
            {
                using (SqlCommand command = new SqlCommand(GetTableDetail(database.Connectionstring), conn))
                {
                    conn.Open();
                    command.CommandTimeout = 0;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        InitTableIndex(reader);
                        InitColIndex(reader);

                        while (reader.Read())
                        {
                            if (lastTableName != (string)reader[TableNameIndex])
                            {
                                lastTableName = (string)reader[TableNameIndex];
                                isTable = reader["ObjectType"].ToString().Trim().Equals("U");
                                if (isTable)
                                {
                                    item = new Table();
                                    item.Name = (string)reader[TableNameIndex];
                                    database.Tables.Add((Table)item);
                                }
                            }

                            if (isTable)
                                FillColumn(item, reader);
                        }
                    }
                }
            }
        }

        public void Execute(Database db, string sql)
        {
            //Check if multiple queries need to be executed
            if (sql.Contains(";"))
            {
                //Parse the string into seperate queries and executed them.
                string[] delimiters = new string[] { ";" };
                string[] queries = sql.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                foreach (string sqlQuery in queries)
                {
                    SqlHelper.ExecuteNonQuery(db.Connectionstring, CommandType.Text, sqlQuery);
                }
            }
            else
            {
                SqlHelper.ExecuteNonQuery(db.Connectionstring, CommandType.Text, sql);
            }
        }

        public string GenAddTableSql(IBucket bucket)
        {
            StringBuilder createBuilder = new StringBuilder();
            StringBuilder primaryKeyList = new StringBuilder();

            FluentBucket fluentBucket = FluentBucket.As(bucket);

            bool hasPrimaryKey = false;

            fluentBucket.For.EachItem.Match(delegate(BucketItem bucketItem)
            {
                return bucketItem.Unique;
            }).Process(delegate(BucketItem bucketItem)
            {
                createBuilder.Append(GenerateDeclaration(bucketItem));
                createBuilder.Append(",\n");

                hasPrimaryKey = true;
                primaryKeyList.Append(bucketItem.Name);
                primaryKeyList.Append(",");
            });

            fluentBucket.For.EachItem.Match(delegate(BucketItem bucketItem)
            {
                return !bucketItem.Unique;
            }).Process(delegate(BucketItem bucketItem)
            {
                createBuilder.Append(GenerateDeclaration(bucketItem));
                createBuilder.Append(",\n");
            });

            string primaryKeyString = string.Empty;

            if (hasPrimaryKey)
            {
                if (primaryKeyList.Length > 0)
                {
                    primaryKeyList.Remove(primaryKeyList.Length - 1, 1);
                }

                primaryKeyString = string.Format(
@"CONSTRAINT [{0}] PRIMARY KEY CLUSTERED
(
  {1} 
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]",
                                        "PK_" + fluentBucket.Entity.Name,
                                        primaryKeyList.ToString());
            }
            createBuilder.Remove(createBuilder.Length - 2, 2);

            string param = createBuilder + ",\r\n" + primaryKeyString;

            return string.Format(
@"IF Not EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}]') AND type in (N'U'))
CREATE TABLE [{0}]
(
	{1}
)ON [PRIMARY];",
               fluentBucket.Entity.Name,
               param);
        }

        public string GenAddColumnSql(string tablename, string columnname, Type columntype)
        {
            return string.Format("ALTER TABLE {0} ADD [{1}] {2}\n",
                            tablename,
                            columnname,
                            GetDbType(columntype));
        }

        public string GenChangeColumnSql(string tablename, string columnname, Type columntype, string oldtype)
        {
            return string.Empty;
        }

        private string GenerateDeclaration(BucketItem item)
        {
            string sql = string.Empty;

            sql += string.Format("[{0}] {1}", item.Name, GetDbType(item.PropertyType));

            if (item.FindAttribute(typeof(PKAttribute)) != null)
                sql += string.Format(" NOT NULL {0}", item.PropertyType == typeof(int) ? "IDENTITY(1,1)" : string.Empty);

            return sql;
        }

        public string GetDbType(Type type)
        {
            switch (type.FullName)
            {
                case "System.String":
                    return "nvarchar(2000)";
                case "System.DateTime":
                    return "datetime";
                case "System.Int32":
                    return "int";
                case "System.Boolean":
                    return "bit";
                case "System.Int64":
                    return "bigint";
                default:
                    return "nvarchar(2000)";
            }
        }

        #region sql

        private static string GetTableDetail(string connstring)
        {
            SqlHelper.Version version = SqlHelper.GetVersion(connstring);

            if (version == SqlHelper.Version.SQLServer2000) return GetTableDetail2000();
            if (version == SqlHelper.Version.SQLServer2005) return GetTableDetail2005();
            if (version == SqlHelper.Version.SQLServer2008) return GetTableDetail2008();
            return "";
        }

        private static string GetTableDetail2008()
        {
            string sql = "";
            sql += "SELECT DISTINCT (CASE WHEN ISNULL(CTT.is_track_columns_updated_on,0) <> 0 THEN is_track_columns_updated_on ELSE 0 END) AS HasChangeTrackingTrackColumn, (CASE WHEN ISNULL(CTT.object_id,0) <> 0 THEN 1 ELSE 0 END) AS HasChangeTracking, TTT.lock_escalation_desc, T.type AS ObjectType, C.Name, C.is_filestream, C.is_sparse, S4.Name as OwnerType,C.user_type_id, C.Column_Id AS ID, C.max_length AS Size, C.Precision, C.Scale, ISNULL(C.Collation_Name,'') as Collation, C.Is_nullable AS IsNullable, C.Is_RowGuidcol AS IsRowGuid, C.Is_Computed AS IsComputed, C.Is_Identity AS IsIdentity, COLUMNPROPERTY(T.object_id,C.name,'IsIdNotForRepl') AS IsIdentityRepl,IDENT_SEED('[' + S1.name + '].[' + T.Name + ']') AS IdentSeed, IDENT_INCR('[' + S1.name + '].[' + T.Name + ']') AS IdentIncrement, ISNULL(CC.Definition,'') AS Formula, ISNULL(CC.Is_Persisted,0) AS FormulaPersisted, CASE WHEN ISNULL(DEP.column_id,0) = 0 THEN 0 ELSE 1 END AS HasComputedFormula, CASE WHEN ISNULL(IC.column_id,0) = 0 THEN 0 ELSE 1 END AS HasIndex, TY.Name AS Type, '[' + S3.Name + '].' + XSC.Name AS XMLSchema, C.Is_xml_document, TY.is_user_defined, ISNULL(TT.Name,T.Name) AS TableName, T.object_id AS ObjectId,S1.name AS TableOwner,Text_In_Row_limit, large_value_types_out_of_row,ISNULL(objectproperty(T.object_id, N'TableHasVarDecimalStorageFormat'),0) AS HasVarDecimal,OBJECTPROPERTY(T.OBJECT_ID,'TableHasClustIndex') AS HasClusteredIndex,DSIDX.Name AS FileGroup,ISNULL(lob.Name,'') AS FileGroupText, ISNULL(filestr.Name,'') AS FileGroupStream,ISNULL(DC.object_id,0) AS DefaultId, DC.name AS DefaultName, DC.definition AS DefaultDefinition, C.rule_object_id, C.default_object_id ,prop.value ";
            sql += "FROM sys.columns C ";
            sql += "INNER JOIN sys.objects T ON T.object_id = C.object_id ";
            sql += "INNER JOIN sys.types TY ON TY.user_type_id = C.user_type_id ";
            sql += "LEFT JOIN sys.indexes IDX ON IDX.object_id = T.object_id and IDX.index_id < 2 ";
            sql += "LEFT JOIN sys.data_spaces AS DSIDX ON DSIDX.data_space_id = IDX.data_space_id ";
            sql += "LEFT JOIN sys.table_types TT ON TT.type_table_object_id = C.object_id ";
            sql += "LEFT JOIN sys.tables TTT ON TTT.object_id = C.object_id ";
            sql += "LEFT JOIN sys.schemas S1 ON (S1.schema_id = TTT.schema_id and T.type = 'U') OR (S1.schema_id = TT.schema_id and T.type = 'TT')";
            sql += "LEFT JOIN sys.xml_schema_collections XSC ON XSC.xml_collection_id = C.xml_collection_id ";
            sql += "LEFT JOIN sys.schemas S3 ON S3.schema_id = XSC.schema_id ";
            sql += "LEFT JOIN sys.schemas S4 ON S4.schema_id = TY.schema_id ";
            sql += "LEFT JOIN sys.computed_columns CC ON CC.column_id = C.column_Id AND C.object_id = CC.object_id ";
            sql += "LEFT JOIN sys.sql_dependencies DEP ON DEP.referenced_major_id = C.object_id AND DEP.referenced_minor_id = C.column_Id AND DEP.object_id = C.object_id ";
            sql += "LEFT JOIN sys.index_columns IC ON IC.object_id = T.object_id AND IC.column_Id = C.column_Id ";
            sql += "LEFT JOIN sys.data_spaces AS lob ON lob.data_space_id = TTT.lob_data_space_id ";
            sql += "LEFT JOIN sys.data_spaces AS filestr ON filestr.data_space_id = TTT.filestream_data_space_id ";
            sql += "LEFT JOIN sys.default_constraints DC ON DC.parent_object_id = T.object_id AND parent_column_id = C.Column_Id ";
            sql += "LEFT JOIN sys.change_tracking_tables CTT ON CTT.object_id = T.object_id ";
            sql += "LEFT JOIN sys.extended_properties prop on prop.major_id = T.object_id and prop.minor_id = C.column_id ";
            sql += "WHERE T.type IN ('U','TT') ";
            sql += "ORDER BY ISNULL(TT.Name,T.Name),T.object_id,C.column_id";
            return sql;
        }

        private static string GetTableDetail2005()
        {
            string sql = "";
            sql += "SELECT DISTINCT T.type AS ObjectType, C.Name, S4.Name as OwnerType,";
            sql += "C.user_type_id, C.Column_Id AS ID, C.max_length AS Size, C.Precision, C.Scale, ISNULL(C.Collation_Name,'') as Collation, C.Is_nullable AS IsNullable, C.Is_RowGuidcol AS IsRowGuid, C.Is_Computed AS IsComputed, C.Is_Identity AS IsIdentity, COLUMNPROPERTY(T.object_id,C.name,'IsIdNotForRepl') AS IsIdentityRepl,IDENT_SEED('[' + S1.name + '].[' + T.Name + ']') AS IdentSeed, IDENT_INCR('[' + S1.name + '].[' + T.Name + ']') AS IdentIncrement, ISNULL(CC.Definition,'') AS Formula, ISNULL(CC.Is_Persisted,0) AS FormulaPersisted, CASE WHEN ISNULL(DEP.column_id,0) = 0 THEN 0 ELSE 1 END AS HasComputedFormula, CASE WHEN ISNULL(IC.column_id,0) = 0 THEN 0 ELSE 1 END AS HasIndex, TY.Name AS Type, '[' + S3.Name + '].' + XSC.Name AS XMLSchema, C.Is_xml_document, TY.is_user_defined, ";
            sql += "T.Name AS TableName, T.object_id AS ObjectId,S1.name AS TableOwner,Text_In_Row_limit, large_value_types_out_of_row,ISNULL(objectproperty(T.object_id, N'TableHasVarDecimalStorageFormat'),0) AS HasVarDecimal,OBJECTPROPERTY(T.OBJECT_ID,'TableHasClustIndex') AS HasClusteredIndex,DSIDX.Name AS FileGroup,ISNULL(LOB.Name,'') AS FileGroupText, ";
            sql += "ISNULL(DC.object_id,0) AS DefaultId, DC.name AS DefaultName, DC.definition AS DefaultDefinition, C.rule_object_id, C.default_object_id ,prop.value ";
            sql += "FROM sys.columns C ";
            sql += "INNER JOIN sys.tables T ON T.object_id = C.object_id ";
            sql += "INNER JOIN sys.types TY ON TY.user_type_id = C.user_type_id ";
            sql += "INNER JOIN sys.schemas S1 ON S1.schema_id = T.schema_id ";
            sql += "INNER JOIN sys.indexes IDX ON IDX.object_id = T.object_id and IDX.index_id < 2 ";
            sql += "INNER JOIN sys.data_spaces AS DSIDX ON DSIDX.data_space_id = IDX.data_space_id ";
            sql += "LEFT JOIN sys.xml_schema_collections XSC ON XSC.xml_collection_id = C.xml_collection_id ";
            sql += "LEFT JOIN sys.schemas S3 ON S3.schema_id = XSC.schema_id ";
            sql += "LEFT JOIN sys.schemas S4 ON S4.schema_id = TY.schema_id ";
            sql += "LEFT JOIN sys.computed_columns CC ON CC.column_id = C.column_Id AND C.object_id = CC.object_id ";
            sql += "LEFT JOIN sys.sql_dependencies DEP ON DEP.referenced_major_id = C.object_id AND DEP.referenced_minor_id = C.column_Id AND DEP.object_id = C.object_id ";
            sql += "LEFT JOIN sys.index_columns IC ON IC.object_id = T.object_id AND IC.column_Id = C.column_Id ";
            sql += "LEFT JOIN sys.data_spaces AS LOB ON LOB.data_space_id = T.lob_data_space_id ";
            sql += "LEFT JOIN sys.default_constraints DC ON DC.parent_object_id = T.object_id AND parent_column_id = C.Column_Id ";
            sql += "LEFT JOIN sys.extended_properties prop on prop.major_id = T.object_id and prop.minor_id = C.column_id ";
            sql += "ORDER BY T.Name,T.object_id,C.column_id";
            return sql;
        }

        private static string GetTableDetail2000()
        {
            return "SELECT TableName=d.name,TableDesc=case when a.colorder=1 then isnull(f.value,'') else '' end, ObjectType=case when a.colorder=1 then 'U' else '' end, Name=a.name, IsIdentity=case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then '√'else '' end, PK=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in ( SELECT name FROM sysindexes WHERE indid in( SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid ))) then 1 else 0 end, Type=b.name, length=COLUMNPROPERTY(a.id,a.name,'PRECISION'),  allowNull=case when a.isnullable=1 then 1 else 0 end,  value=isnull(g.[value],'')  FROM syscolumns a  left join systypes b on a.xusertype=b.xusertype  inner join sysobjects d on a.id=d.id and d.xtype='U' and d.name<>'dtproperties'  left join syscomments e on a.cdefault=e.id  left join sysproperties g on a.id=g.id and a.colid=g.smallid  left join sysproperties f on d.id=f.id and f.smallid=0 order by TableName,ObjectType desc";
        }

        #endregion

        #endregion

        public List<QueryObject<T>> BulkCopy<T>(string connstring, Bucket bucket, IList<QueryObject<T>> items) where T : IQueryObject, new()
        {
            List<QueryObject<T>> list = new List<QueryObject<T>>(items);

            if (items.Count == 0) return list;

            DataTable dt = new DataTable(bucket.Name);
            foreach (var item in bucket.Items.Values)
            {
                Type t = item.PropertyType;
                if (Nullable.GetUnderlyingType(item.PropertyType) != null)
                    t = Nullable.GetUnderlyingType(item.PropertyType);
                dt.Columns.Add(item.Name, t);
            }

            foreach (var item in items)
            {
                if (!item.IsNewlyAdded)
                    continue;

                DataRow row = dt.NewRow();

                foreach (var bi in item.FillBucket(bucket).Items.Values)
                {
                    if (bi.Value == null)
                        row[bi.Name] = DBNull.Value;
                    else if (bi.PropertyType == typeof(DateTime) && ((DateTime)bi.Value == DateTime.MinValue || (DateTime)bi.Value == DateTime.MaxValue))
                        row[bi.Name] = DBNull.Value;
                    else
                        row[bi.Name] = bi.Value;
                }

                dt.Rows.Add(row);
            }

            if (dt.Rows.Count == 0) return list;

            using (SqlConnection conn = new SqlConnection(connstring))
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, null);

                foreach (DataColumn column in dt.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                bulkCopy.DestinationTableName = dt.TableName;
                bulkCopy.BatchSize = dt.Rows.Count;

                try
                {
                    conn.Open();

                    bulkCopy.WriteToServer(dt);
                }
                finally
                {
                    if (bulkCopy != null)
                        bulkCopy.Close();

                    conn.Close();
                }
            }

            list.RemoveAll((i) => { return i.IsNewlyAdded; });
            return list;
        }
    }
}
