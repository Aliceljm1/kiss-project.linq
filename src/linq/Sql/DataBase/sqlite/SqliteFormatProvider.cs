using System;
using Kiss.Linq.Fluent;

namespace Kiss.Linq.Sql
{
    /// <summary>
    /// sql lite format provider
    /// </summary>
    public class SqliteFormatProvider : TSqlFormatProvider
    {
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

        public override string AddItemFormat()
        {
            return @"INSERT INTO [${Entity}] ( ${TobeInsertedFields}) VALUES (${TobeInsertedValues}); SELECT * FROM [${Entity}] WHERE ${UniqueItem} = last_insert_rowid();";
        }

        public override string GetDateTimeValue(DateTime dt)
        {
            return dt.ToString("s");
        }
    }
}
