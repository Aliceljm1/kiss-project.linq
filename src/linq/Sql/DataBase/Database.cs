using System;
using System.Collections.Generic;
using System.Text;
using Kiss.Linq.Fluent;

namespace Kiss.Linq.Sql.DataBase
{
    public class Database
    {
        IDDL ddl = null;

        public Database(IDDL ddl, string connstring)
        {
            Tables = new List<Table>();
            this.ddl = ddl;
            Connectionstring = connstring;
            ddl.Init(Connectionstring);
        }

        public string Connectionstring { get; private set; }

        public List<Table> Tables { get; set; }

        public void Fill()
        {
            if (ddl != null)
                ddl.Fill(this);
        }

        public Table FindTable(string table_name)
        {
            return Tables.Find(delegate(Table t)
            {
                return t.Name.Equals(table_name, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        public void Execute(string sql)
        {
            if (string.IsNullOrEmpty(sql) || ddl == null)
                return;

            LogManager.GetLogger<Database>().Debug(sql);

            ddl.Execute(this, sql);
        }

        public string GenerateSql(Type type)
        {
            BucketImpl bucket = new BucketImpl(type).Describe();

            Table table = FindTable(bucket.Name);
            // if table not exist, generate table sql
            if (table == null)
            {
                return ddl.GenAddTableSql(bucket);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                FluentBucket.As(bucket).For.EachItem.Process(delegate(BucketItem bucketItem)
                {
                    Column column = table.FindColumn(bucketItem.Name);

                    // if column not exist , generate add column sql
                    if (column == null)
                        sb.Append(ddl.GenAddColumnSql(bucket.Name, bucketItem.Name, bucketItem.PropertyType));
                    // if column type changed, generate modify column sql
                    else
                        sb.Append(ddl.GenChangeColumnSql(bucket.Name, bucketItem.Name, bucketItem.PropertyType, column.Type));
                });

                return sb.ToString();
            }
        }
    }

    public class Table
    {
        public Table()
        {
            Columns = new List<Column>();
        }

        public string Name { get; set; }

        public List<Column> Columns { get; set; }

        public Column FindColumn(string column_name)
        {
            return Columns.Find(delegate(Column column)
            {
                return column.Name.Equals(column_name, StringComparison.InvariantCultureIgnoreCase);
            });
        }
    }

    public class Column
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Desc { get; set; }

        public string NetTypeString
        {
            get
            {
                string sqlType = Type;

                if (sqlType.Equals("bigint")) return "long";
                if (sqlType.Equals("int")) return "int";
                if (sqlType.Equals("smallint")) return "short";
                if (sqlType.Equals("tinyint")) return "byte";
                if (sqlType.Equals("bit")) return "bool";
                if (sqlType.Equals("decimal")) return "System.Decimal";
                if (sqlType.Equals("numeric")) return "System.Decimal";
                if (sqlType.Equals("money")) return "System.Decimal";
                if (sqlType.Equals("smallmoney")) return "System.Decimal";
                if (sqlType.Equals("float")) return "float";
                if (sqlType.Equals("real")) return "double";
                if (sqlType.Equals("datetime")) return "DateTime";
                if (sqlType.Equals("smalldatetime")) return "DateTime";
                if (sqlType.Equals("varchar")) return "string";
                if (sqlType.Equals("nchar")) return "string";
                if (sqlType.Equals("char")) return "string";
                if (sqlType.Equals("text")) return "string";
                if (sqlType.Equals("nvarchar")) return "string";
                if (sqlType.Equals("ntext")) return "string";
                if (sqlType.Equals("binary")) return "byte[]";
                if (sqlType.Equals("varbinary")) return "byte[]";
                if (sqlType.Equals("image")) return "byte[]";
                if (sqlType.Equals("timestamp")) return "long";
                if (sqlType.Equals("uniqueidentifier")) return "Guid";
                if (sqlType.Equals("xml")) return "string";
                throw new Exception("Unexpected data type: " + sqlType);
            }
        }
    }
}
