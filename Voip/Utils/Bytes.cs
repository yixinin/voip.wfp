using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voip.Utils
{
    public class Bytes
    {

        public static UInt32 BytesToUint32(byte[] bs)
        {
            return BitConverter.ToUInt32(bs, 0);
        }
        public static ulong BytesToUint64(byte[] bs)
        {
            return BitConverter.ToUInt64(bs, 0);
        }
        public static int BytesToInt32(byte[] bs)
        {
            return BitConverter.ToInt32(bs, 0);
        }
        public static byte[] Uint32ToBytes(int i)
        {
            return BitConverter.GetBytes((uint)(i));
        }

        public static byte[] Uint64ToBytes(long i)
        {
            return BitConverter.GetBytes((ulong)(i));
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
        public static byte[] Int32ToBytes(int i)
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

        public static long GetTimeStamp(byte[] header)
        {
            var len = new byte[8];
            Array.Copy(header, 2 + 4, len, 0, 8);

            return (int)BytesToUint64(len);

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

        public static byte[] GetJoinBuffer(string token, int roomId)
        {
            var buf = new byte[2 + 32 + 4];
            buf[0] = 2;
            buf[1] = 0;
            var ts = Encoding.UTF8.GetBytes(token);
            var rs = Int64ToBytes(roomId);

            Array.Copy(ts, 0, buf, 2, 32);
            Array.Copy(rs, 0, buf, 32 + 2, 4);

            return buf;
        }

        public static byte[] GetAudioBuffer(byte[] body)
        {
            var buf = new byte[body.Length + 2+4+8];
            buf[0] = 2;
            buf[1] = 1;
            var sizes = Uint32ToBytes(body.Length);
            var ts = Uint64ToBytes(Util.GetTimeStamp());
            Array.Copy(sizes, 0, buf, 2, 4);
            Array.Copy(ts, 0, buf, 2 + 4, 8);
            Array.Copy(body, 0, buf, 2 + 4 + 8, body.Length);

            return buf;
        }
        public static byte[] GetVideoBuffer(byte[] body)
        {
            var buf = new byte[body.Length + 2 + 4 + 8];
            buf[0] = 2;
            buf[1] = 2;
            var sizes = Uint32ToBytes(body.Length);
            var ts = Uint64ToBytes(Util.GetTimeStamp());
            Array.Copy(sizes, 0, buf, 2, 4);
            Array.Copy(ts, 0, buf, 2 + 4, 8);
            Array.Copy(body, 0, buf, 2 + 4 + 8, body.Length);

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
