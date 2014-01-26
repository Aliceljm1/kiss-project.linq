using System;
using Kiss;
using Kiss.Validation;

namespace Kiss.Linq.Linq2Sql.Test
{
    /// <summary>
    /// 测试id为字符串，字段类型有：bool类型，decimal类型，
    /// </summary>
    [OriginalName("booktype")]
    public class BookType : QueryObject<BookType, string>
    {
        [PK(AutoGen = false)]
        public override string Id { get { return base.Id; } set { base.Id = value; } }

        /// <summary>
        ///典型书籍id
        /// </summary>
        public string BoolId { get; set; }

        /// <summary>
        /// 书籍分类名
        /// </summary>
        [NotNull]
        public string TypeName { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 类型编码，如1.23
        /// </summary>
        public decimal TypeNum { get; set; }

    }
}
