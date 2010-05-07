
using Kiss;

namespace Kiss.Linq.Linq2Sql.Test
{
    public class Shelve : IQueryObject
    {
        [UniqueIdentifier]
        public int Id { get; set; }

        [OriginalFieldName("Library_Id")]
        public int LibradyId { get; set; }
   
        public string ShelveNo { get; set; }
        
        [OriginalFieldName("sh_Row")]
        public int Row { get; set; }
        
        [OriginalFieldName("sh_Column")]
        public int Column { get; set; }
    }
}
