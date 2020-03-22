
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class Util
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

        public static string GetDeviceCode()
        {
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            foreach (ManagementObject mo in searcher.Get())
            {
                return mo["SerialNumber"].ToString().Trim();
            }
            return "";
        }

        public static UInt32 BytesToUint32(byte[] bs)
        {
            return BitConverter.ToUInt32(bs, 0);
        }
        public static int BytesToInt32(byte[] bs)
        {
            return BitConverter.ToInt32(bs, 0);
        }
        public static byte[] Uint32ToBytes(int i)
        {
            //var buf = new byte[4];
            return BitConverter.GetBytes((uint)(i));
        }

        public static byte[] Uint16ToBytes(int i)
        {
            //var buf = new byte[4];
            return BitConverter.GetBytes((UInt16)(i));
        }

        public static UInt32 BytesToUint16(byte[] bs)
        {
            return BitConverter.ToUInt16(bs, 0);
        }

        public static byte[] Int64ToBytes(long i)
        {
            return BitConverter.GetBytes(i);
        }
        public static byte[] IntToBytes(int i)
        {
            return BitConverter.GetBytes(i);
        }
        public static int GetBufSize(byte[] header)
        {
            var len = new byte[4];
            Array.Copy(header, 2, len, 0, 4);

            return (int)BytesToUint32(len);
        }
        public static byte[] GetAudioHeader(int bodySize)
        {
            var header = new byte[6];
            header[1] = 1;
            header[0] = 2;
            var sizes = Uint32ToBytes(bodySize);
            Array.Copy(sizes, 0, header, 2, 4);

            return header;
        }
        public static byte[] GetVideoHeader(int bodySize)
        {
            var header = new byte[6];
            header[1] = 2;
            header[0] = 2;
            var sizes = Uint32ToBytes(bodySize);
            Array.Copy(sizes, 0, header, 2, 4);

            return header;
        }

        public static byte[] GetJoinBuffer(string token, long roomId)
        {
            var buf = new byte[2 + 32 + 8];
            buf[0] = 2;
            buf[1] = 0;
            var ts = Encoding.UTF8.GetBytes(token);
            var rs = Int64ToBytes(roomId);

            Array.Copy(ts, 0, buf, 2, 32);
            Array.Copy(rs, 0, buf, 32 + 2, 8);

            return buf;
        }

        public static byte[] GetAudioBuffer(byte[] body)
        {
            var buf = new byte[body.Length + 6];
            buf[0] = 2;
            buf[1] = 1;
            var sizes = Uint32ToBytes(body.Length);
            Array.Copy(sizes, 0, buf, 2, 4);
            Array.Copy(body, 0, buf, 6, body.Length);

            return buf;
        }
        public static byte[] GetVideoBuffer(byte[] body)
        {
            var buf = new byte[body.Length + 6];
            buf[0] = 2;
            buf[1] = 2;
            var sizes = Uint32ToBytes(body.Length);
            Array.Copy(sizes, 0, buf, 2, 4);
            Array.Copy(body, 0, buf, 6, body.Length);

            return buf;
        }
        public static string GetBufferText(byte[] buf)
        {
            return string.Format("[{0}]", string.Join(" ", buf));
        }

        public static List<byte[]> SplitH264Buffer(byte[] buf)
        {
            var bodySize = 0;
            var list = new List<byte[]>();
            var frameStart = 0;
            var i = 0;

            var frame = 0;

            while (i < buf.Length - 5)
            {
                if (buf[i] == 0 && buf[i + 1] == 0 && buf[i + 2] == 0 && buf[i + 3] == 1) // 0001
                {
                    if (buf[i + 4] == 104)
                    {
                        i += 4;
                        continue;
                    }
                    if (i > 0)
                    {
                        var body = new byte[i - frameStart];
                        Array.Copy(buf, frameStart, body, 0, body.Length);

                        list.Add(body);

                        bodySize += body.Length;
                    }

                    frameStart = i;
                    frame++;
                    i = i + 4;

                    continue;
                }
                if (buf[i] == 0 && buf[i + 1] == 0 && buf[i + 2] == 1) // 001
                {
                    if (i > 0)
                    {
                        var body = new byte[i - frameStart];
                        Array.Copy(buf, frameStart, body, 0, body.Length);

                        list.Add(body);
                        bodySize += body.Length;
                    }

                    frameStart = i;
                    frame++;
                    i += 3;

                    continue;
                }
                i++;
            }
            if (frameStart != buf.Length - 1)
            {
                var body = new byte[buf.Length - frameStart];
                Array.Copy(buf, frameStart, body, 0, body.Length);
                list.Add(body);
                frame++;
                bodySize += body.Length;
            }

            return list;
        }


        public static byte[] GetBitmapData(Bitmap frameBitmap)
        {
            var bitmapData = frameBitmap.LockBits(new Rectangle(Point.Empty, frameBitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var length = bitmapData.Stride * bitmapData.Height;
                var data = new byte[length];
                Marshal.Copy(bitmapData.Scan0, data, 0, length);
                return data;
            }
            finally
            {
                frameBitmap.UnlockBits(bitmapData);
            }
        }
        public static void copy(float[] x, int x_offset, float[] y, int L)
        {
            copy(x, x_offset, y, 0, L);
        }

        public static void copy(float[] x, int x_offset, float[] y, int y_offset, int L)
        {
            int i;

            for (i = 0; i < L; i++)
                y[y_offset + i] = x[x_offset + i];
        }


    }
}
