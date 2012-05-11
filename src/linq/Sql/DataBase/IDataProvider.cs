using System.Data;

namespace Kiss.Linq.Sql.DataBase
{
    /// <summary>
    /// database related stuffs
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// ExecuteNonQuery
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        int ExecuteNonQuery(string connstring, string sql);

        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        object ExecuteScalar(IDbTransaction tran, string sql);

        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        object ExecuteScalar(string connstring, string sql);

        /// <summary>
        /// ExecuteNonQuery
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        int ExecuteNonQuery(IDbTransaction tran, string sql);

        /// <summary>
        /// ExecuteReader
        /// </summary>
        IDataReader ExecuteReader(string connstring, string sql);

        /// <summary>
        /// ExecuteReader
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        IDataReader ExecuteReader(IDbTransaction tran, string sql);

        DataTable ExecuteDataTable(string connstring, string sql);
        DataTable ExecuteDataTable(IDbTransaction tran, string sql);

        /// <summary>
        /// sql format provider
        /// </summary>
        IFormatProvider GetFormatProvider(string connStr);
    }
}
