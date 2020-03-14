using FFmpeg.AutoGen;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Voip
{
    internal static class FFmpegHelper
    {
        public static unsafe string av_strerror(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }

        public static int ThrowExceptionIfError(this int error, string msg = "")
        {
            if (error < 0)
            {
                if (msg != "") Debug.WriteLine(msg);
                {
                    throw new ApplicationException(av_strerror(error));
                }

            }
            return error;
        }
    }
}