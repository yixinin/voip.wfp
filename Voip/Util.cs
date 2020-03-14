using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static List<byte[]> SplitH264Buffer(byte[] buf)
        {
            var bodySize = 0;
            var list = new List<byte[]>();
            var frameStart = 0;
            var i = 0;

            while (i < buf.Length - 4)
            {
                if (buf[i] == 0 && buf[i + 1] == 0 && buf[i + 2] == 0 && buf[i + 3] == 1) // 0001
                {
                    if (i > 0)
                    {
                        var body = new byte[i - frameStart];
                        Array.Copy(buf, frameStart, body, 0, body.Length);
                        list.Add(body);
                        bodySize += body.Length;

                    }

                    frameStart = i;
                    i = i + 4;

                    continue;
                }
                if (buf[i] == 0 && buf[i + 1] == 0 && buf[i + 3] == 1) // 001
                {
                    if (i > 0)
                    {
                        var body = new byte[i - frameStart];
                        Array.Copy(buf, frameStart, body, 0, body.Length);
                        list.Add(body);
                        bodySize += body.Length;
                    }

                    frameStart = i;
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
        public static void ConfigureHWDecoder(out AVHWDeviceType HWtype)
        {
            HWtype = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE; 
            var availableHWDecoders = new Dictionary<int, AVHWDeviceType>();
            var type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
            var number = 1;
            while ((type = ffmpeg.av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                availableHWDecoders.Add(number, type);
                Debug.WriteLine(String.Format("{0} -> {1}", number, type));
                number++;
            }
            if (availableHWDecoders.Count == 0)
            {
                Console.WriteLine("Your system have no hardware decoders.");
                HWtype = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
                return;
            }
            int decoderNumber = availableHWDecoders.SingleOrDefault(t => t.Value == AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2).Key;
            if (decoderNumber == 0)
                decoderNumber = availableHWDecoders.First().Key;
            var inputDecoderNumber = 2;
            availableHWDecoders.TryGetValue(inputDecoderNumber == 0 ? decoderNumber : inputDecoderNumber, out HWtype);
        }

        public static AVPixelFormat GetHWPixelFormat(AVHWDeviceType hWDevice)
        {
            switch (hWDevice)
            {
                case AVHWDeviceType.AV_HWDEVICE_TYPE_NONE:
                    return AVPixelFormat.AV_PIX_FMT_NONE;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU:
                    return AVPixelFormat.AV_PIX_FMT_VDPAU;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA:
                    return AVPixelFormat.AV_PIX_FMT_CUDA;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI:
                    return AVPixelFormat.AV_PIX_FMT_VAAPI;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2:
                    return AVPixelFormat.AV_PIX_FMT_NV12;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_QSV:
                    return AVPixelFormat.AV_PIX_FMT_QSV;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX:
                    return AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA:
                    return AVPixelFormat.AV_PIX_FMT_NV12;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_DRM:
                    return AVPixelFormat.AV_PIX_FMT_DRM_PRIME;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL:
                    return AVPixelFormat.AV_PIX_FMT_OPENCL;
                case AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC:
                    return AVPixelFormat.AV_PIX_FMT_MEDIACODEC;
                default:
                    return AVPixelFormat.AV_PIX_FMT_NONE;
            }
        }

    }
}
