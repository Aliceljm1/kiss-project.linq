using Kiss.Linq.Fluent;
using System;
using System.Linq;

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

        public override string GetValue(object obj)
        {
            if (obj == null)
                return "''";

            if (Number_Types.Contains(obj.GetType().Name))
                return obj.ToString();

            if (obj is bool)
                return (Convert.ToBoolean(obj) ? 1 : 0).ToString();

            Type type = obj.GetType();
            if (type.IsEnum)
                return ((int)obj).ToString();

            if (obj is DateTime)
                return string.Format("'{0}'", GetDateTimeValue(Convert.ToDateTime(obj)));

            return string.Format("'{0}'", Escape(Convert.ToString(obj)));
        }
    }
}
