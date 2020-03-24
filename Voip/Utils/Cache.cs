using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Utils
{
    public class Cache
    {
        public const string Username = "uname";
        public const string DeviceCode = "device";


        public static string GetDeviceCode()
        {
            try
            {
                string settingString = ConfigurationManager.AppSettings[DeviceCode].ToString();
                return settingString;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static void CacheDeviceCode(string deviceCode)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (ConfigurationManager.AppSettings[DeviceCode] != null)
            {
                config.AppSettings.Settings.Remove(DeviceCode);
            }
            config.AppSettings.Settings.Add(DeviceCode, deviceCode);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static string GetDefaultUserName()
        {
            try
            {
                string settingString = ConfigurationManager.AppSettings[Username].ToString();
                return settingString;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static void SetDefaultUsername(string username)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (ConfigurationManager.AppSettings[Username] != null)
            {
                config.AppSettings.Settings.Remove(Username);
            }
            config.AppSettings.Settings.Add(Username, username);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }



        public static UserInfo GetUserInfo(string username)
        {
            var filename = username + ".cache";
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Open, storage))
                using (StreamReader reader = new StreamReader(stream))
                {

                    var text = reader.ReadToEnd();
                    var info = Newtonsoft.Json.JsonConvert.DeserializeObject<UserInfo>(text);
                    return info;
                }
            }
            catch (FileNotFoundException ex)
            {
                // Handle when file is not found in isolated storage:
                // * When the first application session
                // * When file has been deleted
                return new UserInfo() { Username = username };
            }
        }

        public static void CacheUserInfo(UserInfo info)
        {
            var text = Newtonsoft.Json.JsonConvert.SerializeObject(info);
            var filename = info.Username + ".cache";
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Create, storage))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(text);
                    writer.Flush();
                }
            }
            catch (FileNotFoundException ex)
            {
                // Handle when file is not found in isolated storage:
                // * When the first application session
                // * When file has been deleted
                return;
            }
        }
    }
}
