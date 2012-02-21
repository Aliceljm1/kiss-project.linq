using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Kiss.Linq.Sql.DataBase
{
    public enum DbMode
    {
        mssql,
        sqlite
    }

    public class ScriptProcessor
    {
        public static string Combine(params string[] sqls)
        {
            return string.Join(";", sqls);
        }

        internal static string GenerateScript(string action, string dbtype, params string[] args)
        {
            string path = "Kiss.Linq.Sql.DataBase." + dbtype + "." + action + ".sql";

            if (!_sqlMap.ContainsKey(path))
            {
                _sqlMap.Add(path, GetScript(path));
            }

            StringBuilder builder = new StringBuilder(_sqlMap[path]);

            for (int index = 0; index < args.Length - 1; index += 2)
            {
                builder.Replace(args[index], args[index + 1]);
            }
            return builder.ToString();
        }

        public static string CreateTableScript(DbMode mode, params string[] args)
        {
            return GenerateScript(CommandName.CreateTable, mode.ToString(), args);
        }

        public static string CreatePrimaryScript(DbMode mode, params string[] args)
        {
            return GenerateScript(CommandName.CreatePrimary, mode.ToString(), args);
        }

        public static string GetScript(string resource)
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                StreamReader reader = new StreamReader(resourceStream);
                return reader.ReadToEnd();
            }
        }

        public class ActionKey
        {
            public static string FIELD = "#FIELD#";
            public static string VALUE = "#VALUE#";
            public const string INSERT = "#INSERT#";
            public const string FIELDS = "#FIELDS#";
            public const string VALUES = "#VALUES#";
            public const string ENTITY = "#ENTITY#";
            public const string PARAMS = "#PARAMS#";
            public const string PRIMARY = "#PRIMARY#";

        }

        public class CommandName
        {
            public const string CreateTable = "CreateTable";
            public const string CreatePrimary = "PkConstant";
        }

        private static IDictionary<string, string> _sqlMap = new Dictionary<string, string>();
    }
}
