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
                        Type baseType = typeof(Obj);

                        List<Type> ddlTypes = new List<Type>();

                        foreach (Type type in objtype.Assembly.GetTypes())
                        {
                            if (!type.IsSubclassOf(baseType) || type.IsAbstract)
                                continue;

                            ddlTypes.Add(type);
                        }

                        try
                        {
                            Database db = new Database(ddl, css.ConnectionString);

                            StringBuilder sql = new StringBuilder();

                            // get db's table and column
                            db.Fill();

                            // get sql
                            foreach (Type type in ddlTypes)
                            {
                                sql.Append(db.GenerateSql(type));
                            }

                            // execute sql
                            db.Execute(sql.ToString());

                            LogManager.GetLogger<DDLPlugin>().Info("sync database table schema ok.", objtype.Assembly.GetName().Name);
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetLogger<DDLPlugin>().Fatal(ex.Message);
                        }
                        finally
                        {
                            foreach (Type type in ddlTypes)
                            {
                                ddl_status.Add(type.Name + css.Name);
                            }
                        }
                    }
                }
            }
        }
    }
}
