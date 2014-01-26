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
        string GenAlterTableSql(IBucket bucket, BucketItem item);
        string GenerateColumnDeclaration(BucketItem item);
    }
}
