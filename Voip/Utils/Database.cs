using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Utils
{
    public class Database
    {
        // 数据库文件夹
        static string DbPath = Path.Combine("livechat", "db");
        // 使用全局静态变量保存连接
        public static SQLiteConnection connection = CreateDatabaseConnection();
        //与指定的数据库(实际上就是一个文件)建立连接
        private static SQLiteConnection CreateDatabaseConnection(string dbName = null)
        {
            if (!string.IsNullOrEmpty(DbPath) && !Directory.Exists(DbPath))
                Directory.CreateDirectory(DbPath);
            dbName = dbName == null ? "database.db" : dbName;
            var dbFilePath = Path.Combine(DbPath, dbName);
            return new SQLiteConnection("DataSource = " + dbFilePath);
        }
        public static void Open(SQLiteConnection connection)
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }
        }

    }
}
