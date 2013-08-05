using System.Data;
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
        IFormatProvider Initialize(IBucket bucket);

        string ProcessFormat();
        string GetItemFormat();
        string AddItemFormat();
        string UpdateItemFormat();
        string RemoveItemFormat();
        string BatchAddItemFormat();
        string BatchAddItemValuesFormat();
        string BatchUpdateItemFormat();
        string BatchRemoveItemFormat();

        string DefineString(string method);

        string DefineBatchTobeInsertedValues(DataRow row);

        string GetValue(object obj);
        string Escape(string value);
    }

    public enum FormatMethod
    {
        Process,
        GetItem,
        AddItem,
        UpdateItem,
        RemoveItem,
        BatchAdd,
        BatchAddValues,
        BatchUpdate,
        BatchRemove
    }
}