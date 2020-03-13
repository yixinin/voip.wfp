using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class VideoPacket
    {
        public Bitmap Bmp { get; set; }
        public VideoPacket(Mat mat)
        {
            this.Bmp = mat.ToBitmap(System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        }
    }

    public class AudioPacket
    {
        public byte[] Data { get; set; }
        public AudioPacket(byte[] buf)
        {
            this.Data = buf;
        }
    }
}
