using System.Text;
using Kiss.Linq.Fluent;

namespace Kiss.Linq.Sql
{
    public class TSql2000FormatProvider : TSqlFormatProvider
    {
        public override string ProcessFormat()
        {
            if (FluentBucket.As(bucket).Entity.ItemsToFetch != null)
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("if exists(select 1 from tempdb..sysobjects where type= 'u' and id = object_id(N'tempdb..#PageIndex')) drop table #PageIndex; CREATE TABLE #PageIndex (IndexId int IDENTITY (1, 1) NOT NULL,TID nvarchar(100) );");

                builder.Append("INSERT INTO #PageIndex (TID) SELECT CAST(${NonUniqueFields} AS nvarchar(100)) FROM ${Entity} ${Where} ${OrderBy}");

                builder.Append(" SELECT * FROM ${Entity}, #PageIndex PageIndex WHERE ${NonUniqueFields} = PageIndex.TID AND PageIndex.IndexID >= ${Skip} AND PageIndex.IndexID < ${Skip} + ${PageLength} ORDER BY PageIndex.IndexID; ");

                builder.Append("if exists(select 1 from tempdb..sysobjects where type= 'u' and name like '#PageIndex%') drop table #PageIndex;");
              
                return builder.ToString();
            }

            return base.ProcessFormat();
        }
    }
}