
namespace Kiss.Linq
{
    public interface IQueryObjectImpl : IQueryObject
    {
        bool IsDeleted { get; set; }
        bool IsNewlyAdded { get; }
        bool IsAltered { get; }

        /// <summary>
        /// fills up the bucket from current object.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        Bucket FillBucket ( Bucket bucket );
        /// <summary>
        ///  fills the object from working bucket.
        /// </summary>
        /// <param name="source"></param>
        void FillObject ( Bucket source );
        /// <summary>
        /// fills up the property of current object.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void FillProperty ( string name, object value );
    }
}
