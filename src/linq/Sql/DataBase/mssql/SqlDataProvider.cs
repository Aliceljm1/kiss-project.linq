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
    public class SqlDataProvider : IDataProvider, Kiss.Query.IQuery, IDDL
    {
        public int ExecuteNonQuery(string connstring, CommandType cmdType, string sql)
        {
            int ret = 0;

            using (DbConnection conn = new SqlConnection(connstring))
            {
                conn.Open();

                DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
                command.CommandType = cmdType;

                ret = command.ExecuteNonQuery();

                conn.Close();
            }

            return ret;
        }

        public IDataReader ExecuteReader(string connstring, CommandType cmdType, string sql)
        {
            DbConnection conn = new SqlConnection(connstring);
            conn.Open();

            DbCommand command = new SqlCommand(sql, (SqlConnection)conn);
            command.CommandType = cmdType;

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        private TSqlFormatProvider formatprovider = new TSqlFormatProvider();

        public IFormatProvider FormatProvider
        {
            get { return formatprovider; }
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

            string sql = string.Format("Select ISNULL(COUNT(*),0) FROM {0} {1}",
                query.TableName,
                query.AppendWhereKeyword && StringUtil.HasText(where) ? where.Insert(0, "where ") : where);

            logger.Debug(sql);

            object obj = SqlHelper.ExecuteScalar(query.ConnectionString,
                    CommandType.Text,
                    sql);

            if (obj == null || obj is DBNull)
                return 0;

            return Convert.ToInt32(obj);
        }

        /// <summary>
        /// get IDataReader from query condition
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public IDataReader GetReader(QueryCondition query)
        {
            string where = query.WhereClause;

            where = query.AppendWhereKeyword && StringUtil.HasText(where) ? where.Insert(0, "where ") : where;
            string orderby = string.Empty;
            if (StringUtil.HasText(query.OrderByClause))
                orderby = string.Format("ORDER BY {0}", query.OrderByClause);

            string sql = string.Empty;

            if (query.Paging)
            {
                int startIndex = query.PageSize * query.PageIndex + 1;
                sql = string.Format("WITH tempTab AS ( SELECT ROW_NUMBER() OVER (Order By {0}) AS Row, {1} from {2} {3})  Select * FROM tempTab Where Row between {4} and {5}",
                    StringUtil.HasText(query.OrderByClause) ? query.OrderByClause : "rand()",
                    query.TableField,
                    query.TableName,
                    where,
                    startIndex,
                    startIndex + query.PageSize - 1);
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

            logger.Debug(sql);

            return SqlHelper.ExecuteReader(query.ConnectionString,
                    CommandType.Text,
                    sql);
        }

        public void Delete(QueryCondition query)
        {
            string where = query.WhereClause;

            string sql = string.Format("DELETE FROM [{0}] {1}",
                query.TableName,
                query.AppendWhereKeyword && StringUtil.HasText(where) ? where.Insert(0, "where ") : where);

            logger.Debug(sql);

            SqlHelper.ExecuteNonQuery(query.ConnectionString,
                CommandType.Text,
                sql);
        }

        #endregion

        #region ddl

        private int colNameIndex = -1;
        private int colTypeIndex = -1;
        private int colDescIndex = -1;

        private int TableIdIndex = -1;
        private int TableNameIndex = -1;

        private void InitTableIndex(Database database, IDataRecord reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (TableIdIndex == -1)
            {
                TableIdIndex = reader.GetOrdinal("TableId");
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
            InitColIndex(reader);
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
            TableIdIndex = -1;
            TableNameIndex = -1;
            // create database if not exist
            SqlConnectionStringBuilder cb = new SqlConnectionStringBuilder(conn_string);

            string database_name = cb.InitialCatalog;
            cb.InitialCatalog = "master";
            using (SqlConnection conn = new SqlConnection(cb.ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(string.Format("IF not EXISTS (SELECT name FROM sys.databases WHERE name = N'{0}') CREATE DATABASE [{0}]", database_name), conn);
                conn.Open();

                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void Fill(Database database)
        {
            int lastObjectId = 0;
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
                        while (reader.Read())
                        {
                            InitTableIndex(database, reader);
                            if (lastObjectId != (int)reader[TableIdIndex])
                            {
                                lastObjectId = (int)reader[TableIdIndex];
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

                primaryKeyString = ScriptProcessor.CreatePrimaryScript(DbMode.mssql, "#PK_NAME#", "PK_" + fluentBucket.Entity.Name, ScriptProcessor.ActionKey.PRIMARY, primaryKeyList.ToString());
            }
            createBuilder.Remove(createBuilder.Length - 2, 2);

            string param = createBuilder + ",\r\n" + primaryKeyString;
            // Create script if necessary
            return ScriptProcessor.CreateTableScript(DbMode.mssql, ScriptProcessor.ActionKey.ENTITY, fluentBucket.Entity.Name, ScriptProcessor.ActionKey.PARAMS, param);
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

            if (item.FindAttribute(typeof(UniqueIdentifierAttribute)) != null)
            {
                sql += " ";
                sql += "NOT NULL IDENTITY(1,1)";
            }
            return sql;
        }

        public string GetDbType(Type type)
        {
            switch (type.FullName)
            {
                case "System.String":
                    return "nvarchar(max)";
                case "System.DateTime":
                    return "datetime";
                case "System.Int32":
                    return "int";
                case "System.Boolean":
                    return "bit";
                case "System.Int64":
                    return "bigint";
                default:
                    return "nvarchar(max)";
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
            sql += "SELECT DISTINCT (CASE WHEN ISNULL(CTT.is_track_columns_updated_on,0) <> 0 THEN is_track_columns_updated_on ELSE 0 END) AS HasChangeTrackingTrackColumn, (CASE WHEN ISNULL(CTT.object_id,0) <> 0 THEN 1 ELSE 0 END) AS HasChangeTracking, TTT.lock_escalation_desc, T.type AS ObjectType, C.Name, C.is_filestream, C.is_sparse, S4.Name as OwnerType,C.user_type_id, C.Column_Id AS ID, C.max_length AS Size, C.Precision, C.Scale, ISNULL(C.Collation_Name,'') as Collation, C.Is_nullable AS IsNullable, C.Is_RowGuidcol AS IsRowGuid, C.Is_Computed AS IsComputed, C.Is_Identity AS IsIdentity, COLUMNPROPERTY(T.object_id,C.name,'IsIdNotForRepl') AS IsIdentityRepl,IDENT_SEED('[' + S1.name + '].[' + T.Name + ']') AS IdentSeed, IDENT_INCR('[' + S1.name + '].[' + T.Name + ']') AS IdentIncrement, ISNULL(CC.Definition,'') AS Formula, ISNULL(CC.Is_Persisted,0) AS FormulaPersisted, CASE WHEN ISNULL(DEP.column_id,0) = 0 THEN 0 ELSE 1 END AS HasComputedFormula, CASE WHEN ISNULL(IC.column_id,0) = 0 THEN 0 ELSE 1 END AS HasIndex, TY.Name AS Type, '[' + S3.Name + '].' + XSC.Name AS XMLSchema, C.Is_xml_document, TY.is_user_defined, ISNULL(TT.Name,T.Name) AS TableName, T.object_id AS TableId,S1.name AS TableOwner,Text_In_Row_limit, large_value_types_out_of_row,ISNULL(objectproperty(T.object_id, N'TableHasVarDecimalStorageFormat'),0) AS HasVarDecimal,OBJECTPROPERTY(T.OBJECT_ID,'TableHasClustIndex') AS HasClusteredIndex,DSIDX.Name AS FileGroup,ISNULL(lob.Name,'') AS FileGroupText, ISNULL(filestr.Name,'') AS FileGroupStream,ISNULL(DC.object_id,0) AS DefaultId, DC.name AS DefaultName, DC.definition AS DefaultDefinition, C.rule_object_id, C.default_object_id ,prop.value ";
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
            sql += "T.Name AS TableName, T.object_id AS TableId,S1.name AS TableOwner,Text_In_Row_limit, large_value_types_out_of_row,ISNULL(objectproperty(T.object_id, N'TableHasVarDecimalStorageFormat'),0) AS HasVarDecimal,OBJECTPROPERTY(T.OBJECT_ID,'TableHasClustIndex') AS HasClusteredIndex,DSIDX.Name AS FileGroup,ISNULL(LOB.Name,'') AS FileGroupText, ";
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
            string sql = "";
            sql += "SELECT SO.name, ";
            sql += "SO.id as object_id, ";
            sql += "SU.name as Owner, ";
            sql += "OBJECTPROPERTY(SO.ID,'TableTextInRowLimit') AS Text_In_Row_limit,";
            sql += "0 AS HasVarDecimal, ";
            sql += "CONVERT(bit,0) AS large_value_types_out_of_row, ";
            sql += "F.groupname AS FileGroup, ";
            sql += "ISNULL(F2.groupname,'') AS FileGroupText, ";
            sql += "OBJECTPROPERTY(SO.ID,'TableHasClustIndex') AS HasClusteredIndex ";
            sql += "FROM sysobjects SO ";
            sql += "inner join sysindexes I ON I.id = SO.id and I.indid < 2 ";
            sql += "inner join sysfilegroups f on f.groupid = i.groupid ";
            sql += "left join sysindexes I2 ON I2.id = SO.id and I2.indid = 255 ";
            sql += "left join sysfilegroups f2 on f2.groupid = i2.groupid ";
            sql += "INNER JOIN sysusers SU ON SU.uid = SO.uid WHERE type = 'U' ORDER BY SO.name";
            return sql;
        }

        #endregion

        #endregion
    }
}
