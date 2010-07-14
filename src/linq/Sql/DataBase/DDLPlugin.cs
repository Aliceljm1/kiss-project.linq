using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Kiss.Plugin;

namespace Kiss.Linq.Sql.DataBase
{
    [Plugin]
    public class DDLPlugin
    {
        // ddl status
        private static readonly List<string> ddl_status = new List<string>();
        private static readonly object _lock = new object();

        public void Sync(IDDL ddl, Type objtype, ConnectionStringSettings css)
        {
            if (ddl == null)
                return;

            string key = objtype.Name + css.Name;

            if (!ddl_status.Contains(key))
            {
                lock (_lock)
                {
                    if (!ddl_status.Contains(key))
                    {
                        try
                        {
                            Database db = new Database(ddl, css.ConnectionString);

                            StringBuilder sql = new StringBuilder();

                            // get db's table and column
                            db.Fill();

                            sql.Append(db.GenerateSql(objtype));

                            // execute sql
                            db.Execute(sql.ToString());

                            LogManager.GetLogger<DDLPlugin>().Info("sync table schema of {0} ok.", objtype.Name);
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetLogger<DDLPlugin>().Fatal(ex.Message);
                        }
                        finally
                        {
                            if (!ddl_status.Contains(key))
                                ddl_status.Add(key);
                        }
                    }
                }
            }
        }
    }
}
