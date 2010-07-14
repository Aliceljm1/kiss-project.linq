using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiss.Linq;

namespace Kiss.Linq.Linq2Sql.Test
{
    [OriginalName("Bk_Library")]
    class Library : IQueryObject
    {
        [PK]
        public int Id { get; set; }
        public string Floor
        {
            get;
            set;
        }
        public string Section { get; set; }
    }
}
