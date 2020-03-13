using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
   public class VideoPacket
    {
        public byte[] Data { get; set; }
        public VideoPacket(byte[] buf)
        {
            this.Data = buf;
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
