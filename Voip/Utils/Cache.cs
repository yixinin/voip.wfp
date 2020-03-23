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
        public const string Password = "pwd";
        public const string Username = "uname";
        public const string Remember = "remem";
        public const string AutoSignIn = "autos";
        public const string Avatar = "avatar";
        public const string DeviceCode = "device";


        public static string GetDefaultUserName()
        {
            try
            {
                var key = "username";
                string settingString = ConfigurationManager.AppSettings[key].ToString();
                return settingString;
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static void SetDefaultUsername(string username)
        {
            var key = Username;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (ConfigurationManager.AppSettings[key] != null)
            {
                config.AppSettings.Settings.Remove(key);
            }
            config.AppSettings.Settings.Add(key, username);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        public static string GetSettingString(string settingName, string username)
        {
            try
            {
                var key = string.Format("{0}_{1}", settingName, username);
                string settingString = ConfigurationManager.AppSettings[key].ToString();
                return settingString;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="settingName"></param>
        /// <param name="valueName"></param>
        public static void UpdateSettingString(string settingName, string username, string valueName)
        {
            var key = string.Format("{0}_{1}", settingName, username);
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (ConfigurationManager.AppSettings[key] != null)
            {
                config.AppSettings.Settings.Remove(key);
            }
            config.AppSettings.Settings.Add(key, valueName);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
