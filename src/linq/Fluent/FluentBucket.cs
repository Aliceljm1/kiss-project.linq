using System.Text;
using Kiss.Utils;

namespace Kiss.Linq.Fluent
{
    /// <summary>
    /// Fluent implementation for the bucket object.
    /// </summary>
    public class FluentBucket
    {
        /// <summary>
        /// Create a new instance of <see cref="FluentBucket"/> for a <see cref="bucket"/>
        /// </summary>
        /// <param name="bucket"></param>
        public FluentBucket ( IBucket bucket )
        {
            this.bucket = bucket;
        }

        /// <summary>
        /// Creates a fluent wrapper of the original bucket object.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns><see cref="FluentBucket"/></returns>
        public static FluentBucket As ( IBucket bucket )
        {
            return new FluentBucket ( bucket );
        }

        /// <summary>
        /// Creates and gets a new fluent entity object.
        /// </summary>
        public FluentEntity Entity
        {
            get
            {
                if ( entity == null )
                {
                    entity = new FluentEntity ( bucket );
                }
                return entity;
            }
        }

        /// <summary>
        /// Gets true if any where clause is used.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                return bucket.IsDirty;
            }
        }

        /// <summary>
        /// Translates the fluent bucket to a equavalant literal format.
        /// </summary>
        /// <param name="formatProvider">fromat provider implemenatation</param>
        /// <returns>translated string</returns>
        public string Translate ( FormatMethod method, IFormatProvider formatProvider )
        {
            formatProvider.Initialize ( bucket );
            
            string selectorString = GetFormatString ( method, formatProvider );

            StringBuilder builder = new StringBuilder ( selectorString );

            foreach ( string format in StringUtil.GetAntExpressions ( selectorString ) )
            {
                builder.Replace ( "${" + format + "}", formatProvider.DefineString ( format ) );
            }

            return builder.ToString ( );
        }

        /// <summary>
        /// contains the bucketItem and their relational info.
        /// </summary>
        public FluentExpressionTree ExpressionTree
        {
            get
            {
                return new FluentExpressionTree ( bucket.CurrentNode );
            }
        }

        /// <summary>
        /// enables BucketItem
        /// </summary>
        public FluentIterator For
        {
            get
            {
                return new FluentIterator ( bucket );
            }
        }

        private IBucket bucket;
        private FluentEntity entity;

        private static string GetFormatString ( FormatMethod method, IFormatProvider formatProvider )
        {
            string selectorString = string.Empty;

            switch ( method )
            {
                case FormatMethod.Process:
                    selectorString = formatProvider.ProcessFormat ( );
                    break;
                case FormatMethod.GetItem:
                    selectorString = formatProvider.GetItemFormat ( );
                    break;
                case FormatMethod.AddItem:                    
                    selectorString = formatProvider.AddItemFormat ( );
                    break;
                case FormatMethod.UpdateItem:
                    selectorString = formatProvider.UpdateItemFormat ( );
                    break;
                case FormatMethod.RemoveItem:
                    selectorString = formatProvider.RemoveItemFormat ( );
                    break;
                case FormatMethod.BatchAdd:
                    selectorString = formatProvider.BatchAddItemFormat();
                    break;
                case FormatMethod.BatchUpdate:
                    selectorString = formatProvider.BatchUpdateItemFormat();
                    break;
                case FormatMethod.BatchRemove:
                    selectorString = formatProvider.BatchRemoveItemFormat();
                    break;
                default:
                    break;
            }

            return selectorString;
        }
    }
}