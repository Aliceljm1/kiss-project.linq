using System;
using Kiss;

namespace Kiss.Linq.Linq2Sql.Test
{
    [OriginalName("book")]
    public class Book : QueryObject<Book, int>
    {
        public string Author { get; set; }

        [OriginalName("Bk_Title")]
        public string Title { get; set; }

        public string ISBN { get; set; }

        public DateTime LastUpdated { get; set; }

        [OriginalName("Shelve_Id")]
        public int ShelveId { get; set; }

        [Ignore]
        public string BookInfo
        {
            get
            {
                return "Published by:" + Author + " With title " + Title;
            }
        }

        /// <summary>
        /// Identity inherits Unique Attribute, these will be useful for update query.
        /// </summary>
        [PK, OriginalName("Bk_Id")]
        override public int Id { get; set; }

        public int GetId()
        {
            return Id;
        }
    }
}
