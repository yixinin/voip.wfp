using System;
using System.Collections.Generic;
using System.Configuration;
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


        public static void CacheUserInfo(UserInfo info)
        {
            var key = "user_local_" + info.Username;
            Data.Save(key, info);
        }

        public static UserInfo GetUserInfo(string username)
        {
            var key = "user_local_" + username;
            var info = Data.Load<UserInfo>(key);
            if(info == null)
            {
                return new UserInfo();
            }
            return info;
        }

    }
}
