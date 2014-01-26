
using Kiss;

namespace Kiss.Linq.Linq2Sql.Test
{
    public class Shelve : QueryObject<Shelve, int>
    {
        [PK]
        public override int Id { get { return base.Id; } set { base.Id = value; } }

        [OriginalName("Library_Id")]
        public int LibradyId { get; set; }

        public string ShelveNo { get; set; }

        [OriginalName("sh_Row")]
        public int Row { get; set; }

        [OriginalName("sh_Column")]
        public int Column { get; set; }
    }
}
