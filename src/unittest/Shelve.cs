
using Kiss;

namespace Kiss.Linq.Linq2Sql.Test
{
    public class Shelve : IQueryObject
    {
        [PK]
        public int Id { get; set; }

        [OriginalName("Library_Id")]
        public int LibradyId { get; set; }
   
        public string ShelveNo { get; set; }

        [OriginalName("sh_Row")]
        public int Row { get; set; }

        [OriginalName("sh_Column")]
        public int Column { get; set; }
    }
}
