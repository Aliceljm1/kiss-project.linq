using Kiss.Linq.Fluent;
using System;

namespace Kiss.Linq.Sql.Sqlite
{
    /// <summary>
    /// sql lite format provider
    /// </summary>
    public class SqliteFormatProvider : TSqlFormatProvider
    {
        protected override string IdentitySelectString
        {
            get
            {
                return "last_insert_rowid()";
            }
        }
        public override string ProcessFormat()
        {
            if (FluentBucket.As(bucket).Entity.ItemsToFetch != null)
            {
                return "Select * from ${Entity} ${Where} ${OrderBy} limit ${Skip},${PageLength}";
            }

            return "Select * from ${Entity} ${Where} ${OrderBy}";
        }

        public override string DefinePageLength()
        {
            return bucket.ItemsToTake == null ? "100" : bucket.ItemsToTake.Value.ToString();
        }

        public override string DefineSkip()
        {
            return FluentBucket.As(bucket).Entity.ItemsToSkipFromStart.ToString();
        }

        public override string GetDateTimeValue(DateTime dt)
        {
            return dt.ToString("s");
        }
    }
}
