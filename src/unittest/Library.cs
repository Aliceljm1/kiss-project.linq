using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiss.Linq;

namespace Kiss.Linq.Linq2Sql.Test
{
    [OriginalEntityName("Bk_Library")]
    class Library : IQueryObject
    {
        [UniqueIdentifier]
        public int Id { get; set; }
        public string Floor
        {
            get;
            set;
        }
        public string Section { get; set; }
    }
}
