using Kiss.Plugin;

namespace Kiss.Linq.Sql.DataBase
{
    [AutoInit(Priority = 9, Title = "数据库模块")]
    public class DataBaseInitializer : IPluginInitializer
    {
        public void Init(ServiceLocator sl, PluginSetting setting)
        {
            if (!setting.Enable) return;

            sl.AddComponent("System.Data.SqlClient", typeof(SqlDataProvider));
            sl.AddComponent("System.Data.SQLite", typeof(SqliteDataProvider));

            sl.AddComponent("kiss.ddl", typeof(DDLPlugin));
        }
    }
}
