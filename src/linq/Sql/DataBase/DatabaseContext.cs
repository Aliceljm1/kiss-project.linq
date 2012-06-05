using System;
using System.Configuration;
using System.Data;
using Kiss.Plugin;
using Kiss.Repository;
using Kiss.Utils;

namespace Kiss.Linq.Sql.DataBase
{
    /// <summary>
    /// use this class to access database
    /// </summary>
    public class DatabaseContext
    {
        public DatabaseContext(ConnectionStringSettings connectionStringSettings, Type modelType)
        {
            Init(connectionStringSettings, modelType);
        }

        private static ILogger _logger;
        private static ILogger logger { get { if (_logger == null) _logger = LogManager.GetLogger(typeof(DatabaseContext)); return _logger; } }

        IDataProvider dbAccess = null;

        string connstring;

        void Init(ConnectionStringSettings connectionStringSettings, Type modelType)
        {
            if (connectionStringSettings == null)
                throw new LinqException("ConnectionStringSettings is null.");

            try
            {
                dbAccess = ServiceLocator.Instance.Resolve(connectionStringSettings.ProviderName) as IDataProvider;
                if (dbAccess == null)
                    throw new NotSupportedException("not supported database");

                connstring = connectionStringSettings.ConnectionString;

                PluginSetting ddlconfig = PluginSettings.Get<RepositoryInitializer>();
                if (ddlconfig == null)
                    return;

                if (is_ddlallowed(StringUtil.Split(ddlconfig["auto_tables"], StringUtil.Comma, true, true), Kiss.QueryObject.GetTableName(modelType)))
                    DDLFactory.Sync(dbAccess as IDDL, modelType, connectionStringSettings);
            }
            catch (Exception ex)
            {
                throw new LinqException("init DatabaseContext failed!", ex);
            }
        }

        private bool is_ddlallowed(string[] allowed, string type)
        {
            if (allowed.Length == 0)
                return false;

            foreach (var item in allowed)
            {
                if (item == "*")
                    return true;

                if (item.StartsWith("*") && type.EndsWith(item.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                    return true;

                if (item.EndsWith("*") && type.StartsWith(item.Substring(0, item.Length - 1), StringComparison.InvariantCultureIgnoreCase))
                    return true;

                if (item.StartsWith("*") && item.EndsWith("*") && type.ToLower().Contains(item.Substring(1, item.Length - 1).ToLower()))
                    return true;

                if (string.Equals(item, type))
                    return true;
            }

            return false;
        }

        public int ExecuteNonQuery(IDbTransaction tran, string sql)
        {
            if (tran == null)
                return ExecuteNonQuery(sql);

            logger.Debug(sql);

            return dbAccess.ExecuteNonQuery(tran, sql);
        }

        public int ExecuteNonQuery(string sql)
        {
            logger.Debug(sql);

            try
            {
                return dbAccess.ExecuteNonQuery(connstring, sql);
            }
            catch (Exception ex)
            {
                throw new LinqException(sql, ex);
            }
        }

        public object ExecuteScalar(IDbTransaction tran, string sql)
        {
            if (tran == null)
                return ExecuteScalar(sql);

            logger.Debug(sql);

            return dbAccess.ExecuteScalar(tran, sql);
        }

        public object ExecuteScalar(string sql)
        {
            logger.Debug(sql);

            try
            {
                return dbAccess.ExecuteScalar(connstring, sql);
            }
            catch (Exception ex)
            {
                throw new LinqException(sql, ex);
            }
        }

        public IDataReader ExecuteReader(IDbTransaction tran, string sql)
        {
            if (tran == null)
                return ExecuteReader(sql);

            logger.Debug(sql);

            return dbAccess.ExecuteReader(tran, sql);
        }

        public IDataReader ExecuteReader(string sql)
        {
            logger.Debug(sql);

            try
            {
                return dbAccess.ExecuteReader(connstring, sql);
            }
            catch (Exception ex)
            {
                throw new LinqException(sql, ex);
            }
        }

        public DataTable ExecuteDataTable(IDbTransaction tran, string sql)
        {
            if (tran == null)
                return ExecuteDataTable(sql);

            logger.Debug(sql);

            return dbAccess.ExecuteDataTable(tran, sql);
        }

        public DataTable ExecuteDataTable(string sql)
        {
            logger.Debug(sql);

            try
            {
                return dbAccess.ExecuteDataTable(connstring, sql);
            }
            catch (Exception ex)
            {
                throw new LinqException(sql, ex);
            }
        }

        public bool SupportBulkCopy { get { return dbAccess.SupportBulkCopy; } }

        public void BulkCopy(DataTable dt)
        {
            logger.Debug("execute bulk copy. total count: {0}", dt.Rows.Count);

            try
            {
                dbAccess.BulkCopy(connstring, dt);
            }
            catch (Exception ex)
            {
                throw new LinqException("execute bulk copy error!", ex);
            }
        }

        public IFormatProvider FormatProvider
        {
            get
            {
                return dbAccess.GetFormatProvider(connstring);
            }
        }
    }
}
