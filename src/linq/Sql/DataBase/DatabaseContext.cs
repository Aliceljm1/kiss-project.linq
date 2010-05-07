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

                DDLPlugin ddl = null;

                try
                {
                    if (PluginSettings.Get<DataBaseInitializer>()["ddl"].ToBoolean())
                        ddl = ServiceLocator.Instance.Resolve<DDLPlugin>();
                }
                catch
                {
                    logger.Info("ddl is disabled.");
                }
                if (ddl != null)
                    ddl.Sync(dbAccess as IDDL, modelType, connectionStringSettings);
            }
            catch (Exception ex)
            {
                throw new LinqException("init DatabaseContext failed!", ex);
            }
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
