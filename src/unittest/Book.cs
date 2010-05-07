using System;
using Kiss;

namespace Kiss.Linq.Linq2Sql.Test
{
    [OriginalEntityName ( "book" )]
    public class Book : IQueryObject
    {
        public string Author { get; set; }

        [OriginalFieldName ( "Bk_Title" )]
        public string Title { get; set; }

        public string ISBN { get; set; }

        public DateTime LastUpdated { get; set; }

        [OriginalFieldName ( "Shelve_Id" )]
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
        [UniqueIdentifier, OriginalFieldName ( "Bk_Id" )]
        public int Id { get; set; }

        public int GetId ( )
        {
            return Id;
        }
    }
}
