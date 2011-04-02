
namespace Kiss.Linq.Sql
{
    /// <summary>
    /// Format provider interface generating literals.
    /// </summary>
    public interface IFormatProvider
    {
        /// <summary>
        /// Initialzes the format provider.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        IFormatProvider Initialize ( IBucket bucket );

        string ProcessFormat ( );
        string GetItemFormat ( );
        string AddItemFormat ( );
        string UpdateItemFormat ( );
        string RemoveItemFormat ( );
        string BatchAddItemFormat();
        string BatchUpdateItemFormat();
        string BatchRemoveItemFormat();

        string DefineString ( string method );
    }

    public enum FormatMethod
    {
        Process,
        GetItem,
        AddItem,
        UpdateItem,
        RemoveItem,
        BatchAdd,
        BatchUpdate,
        BatchRemove
    }
}