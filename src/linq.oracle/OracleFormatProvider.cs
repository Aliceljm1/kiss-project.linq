using Kiss.Linq.Fluent;

namespace Kiss.Linq.Sql.Oracle
{
    /// <summary>
    /// sql lite format provider
    /// </summary>
    public class OracleFormatProvider : TSqlFormatProvider
    {
        protected override char OpenQuote
        {
            get
            {
                return ' ';
            }
        }

        protected override char CloseQuote
        {
            get
            {
                return ' ';
            }
        }

        /// <summary>
        /// oracle 执行多条语句需要使用begin，end;
        /// </summary>
        /// <returns></returns>
        public override string AddItemFormat()
        {
            return @"Declare ct integer; begin INSERT INTO ${Entity} ( ${TobeInsertedFields} ) VALUES (${TobeInsertedValues}); SELECT count(*) into ct  FROM ${Entity} ${AfterInsertWhere}; end;";
        }

        public override string UpdateItemFormat()
        {
            return @"Declare ct integer; begin Update ${Entity} SET ${UpdateItems} WHERE ${UniqueWhere}; SELECT count(*) into ct FROM ${Entity} Where ${UniqueWhere}; end;";
        }

        public override string Escape(string value)
        {
            return value.Replace(@"\", @"\\").Replace("'", "''").Trim();
        }

        protected override string IdentitySelectString
        {
            get
            {
                return "last_insert_id()";//mysql 获取自动增长字段最新值
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

        public override string DefineAfterInsertWhere()
        {
            string value = string.Empty;
            return value;
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
