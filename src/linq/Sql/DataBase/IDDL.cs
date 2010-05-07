
using System;
using Kiss.Linq;

namespace Kiss.Linq.Sql.DataBase
{
    public interface IDDL
    {
        void Init(string conn_string);
        void Fill(Database db);
        void Execute(Database db, string sql);

        string GenAddTableSql(IBucket bucket);
        string GenAddColumnSql(string tablename, string columnname, Type columntype);
        string GenChangeColumnSql(string tablename, string columnname, Type columntype, string oldtype);

        string GetDbType(Type type);
    }
}
