
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Utils
{
    public class Data
    {


        private static string dir = "data";
        public static void Save<T>(string key, T value)
        {
            var filename = Path.Combine(dir, key + ".cache");
            var text = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            using (var fs = File.OpenWrite(filename))
            {
                var buf = Encoding.UTF8.GetBytes(text);
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
            }
        }
        public static T Load<T>(string key) where T : new()
        {
            var filename = Path.Combine(dir, key + ".cache");
            if (!File.Exists(filename))
            {
                return new T();
            }
            using (var fs = File.Open(filename, FileMode.Open))
            {
                var buf = new byte[fs.Length];

                var n = fs.Read(buf, 0, buf.Length);
                if (n != buf.Length)
                {
                    Debug.WriteLine("read buf error");
                }
                var text = Encoding.UTF8.GetString(buf);
                var t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
                return t;
            }

        }
        //private static string dbPath = string.Empty;
        //private static string DbPath
        //{
        //    get
        //    {
        //        if (string.IsNullOrEmpty(dbPath))
        //        {
        //            dbPath = Path.Combine("db", "Sqlite.db");
        //        }

        //        return dbPath;
        //    }
        //}

        //private static SQLiteConnection DbConnection
        //{
        //    get
        //    {

        //        return new SQLiteConnection(DbPath);
        //    }
        //}

    }
}
