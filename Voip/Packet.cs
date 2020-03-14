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

    public class VideoH264Packet
    {
        public byte[] Buffer { get; set; }
        private bool _ispps;
        public bool IsPPS
        {
            get
            {
                return _ispps;
            }
        }
        public VideoH264Packet(byte[] buf)
        {
            Buffer = buf;
            if (buf != null && buf.Length > 4 && buf[4] == 103)
            {
                _ispps = true;
            }
        }
    }


    public class VideoPacket
    {
        public Bitmap Bmp { get; set; }
        public VideoPacket(Mat mat)
        {
            var rect = new Rect(160, 20, 320, 320);
            var RectMat = new Mat(mat, rect);

            this.Bmp = BitmapConverter.ToBitmap(RectMat, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
