using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kiss.Linq;

namespace Kiss.Linq.Linq2Sql.Test
{
    [OriginalName("Bk_Library")]
    class Library : QueryObject<Library, int>
    {
        [PK]
        public override int Id { get { return base.Id; } set { base.Id = value; } }
        public string Floor
        {
            get;
            set;
        }
        public string Section { get; set; }
    }
}
