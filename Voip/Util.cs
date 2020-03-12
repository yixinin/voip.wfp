using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class Util
    {
        public static UInt32 BytesToUint32(byte[] bs)
        {
            return BitConverter.ToUInt32(bs, 0);
        }
        public static byte[] Uint32ToBytes(int i)
        {
            //var buf = new byte[4];
            return BitConverter.GetBytes((uint)(i));
        }
        public static byte[] Int64ToBytes(long i)
        {
            //var buf = BitConverter.GetBytes(i);
            //var buf = new byte[8];
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
    }
}
