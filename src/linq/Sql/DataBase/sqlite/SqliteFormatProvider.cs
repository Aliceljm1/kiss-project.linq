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
            return @"INSERT INTO [${Entity}] ( ${TobeInsertedFields}) VALUES (${TobeInsertedValues}); SELECT * FROM [${Entity}] ${AfterInsertWhere};";
        }

        public override string DefineAfterInsertWhere()
        {
            string value = string.Empty;

            FluentBucket.As(bucket).For.EachItem
                .Process(delegate(BucketItem item)
                {
                    if (item.Unique)
                    {
                        if ((item.FindAttribute(typeof(PKAttribute)) as PKAttribute).AutoIncrement)
                            value = "WHERE [" + item.Name + "] = last_insert_rowid()";
                        else if (item.Value != null)
                            value = "WHERE [" + item.Name + "] = " + GetValue(item.Value);
                    }
                });

            return value;
        }

        public override string GetDateTimeValue(DateTime dt)
        {
            return dt.ToString("s");
        }
    }
}
