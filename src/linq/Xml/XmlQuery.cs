using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kiss.Linq.Xml
{
    public class XmlQuery<T> : Query<T>, ILinqQuery<T>
          where T : class, IQueryObject
    {
        public bool EnableQueryEvent { get; set; }

        public void SubmitChanges(bool batch)
        {

        }

        protected override T GetItem(IBucket bucket)
        {
            return base.GetItem(bucket);
        }
    }
}
