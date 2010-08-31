using System;
using System.Configuration;
using System.Data;
using Kiss.Plugin;
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

        #region events

        public static event EventHandler<QueryEventArgs> PreQuery;

        internal virtual void OnPreQuery(QueryEventArgs e)
        {
            EventHandler<QueryEventArgs> handler = PreQuery;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class QueryEventArgs : EventArgs
        {
            public static readonly new QueryEventArgs Empty = new QueryEventArgs();

            public Type Type { get; set; }
            public string Sql { get; set; }
            public object Result { get; set; }
        }

        public static event EventHandler<QueryEventArgs> AfterQuery;

        internal virtual void OnAfterQuery(QueryEventArgs e)
        {
            EventHandler<QueryEventArgs> handler = AfterQuery;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

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

                if (is_ddlallowed(StringUtil.Split(ddlconfig["ddl_types"], StringUtil.Comma, true, true), Kiss.QueryObject.GetTableName(modelType)))
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

        public int ExecuteNonQuery(CommandType cmdType, string sql)
        {
            if (logger != null)
                logger.Debug(sql);

            return dbAccess.ExecuteNonQuery(connstring, cmdType, sql);
        }

        public IDataReader ExecuteReader(CommandType cmdType, string sql)
        {
            if (logger != null)
                logger.Debug(sql);

            return dbAccess.ExecuteReader(connstring, cmdType, sql);
        }

        private IFormatProvider formatProvider;
        public IFormatProvider FormatProvider
        {
            get
            {
                if (formatProvider == null)
                {
                    formatProvider = dbAccess.FormatProvider;
                }
                return formatProvider;
            }
        }
    }
}
