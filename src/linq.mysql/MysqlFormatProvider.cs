using System;
using Kiss.Linq.Fluent;

namespace Kiss.Linq.Sql.Mysql
{
    /// <summary>
    /// sql lite format provider
    /// </summary>
    public class MysqlFormatProvider : TSqlFormatProvider
    {
        protected override char OpenQuote
        {
            get
            {
                return '`';
            }
        }

        protected override char CloseQuote
        {
            get
            {
                return '`';
            }
        }

        protected override string IdentitySelectString
        {
            get
            {
                return "last_insert_id()";
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
    }
}
