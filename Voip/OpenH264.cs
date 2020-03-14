using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class OpenH264Decoder
    {

        public int Fps { get; set; }
        public OpenH264Decoder()
        {

        }
        public void Decode(MemoryStream ms)
        {
            var decoder = new OpenH264Lib.Decoder("openh264-2.0.0-win64.dll");

            //var aviFile = System.IO.File.OpenRead(path);
            var riff = new RiffFile(ms);
           

            var frames = riff.Chunks.OfType<RiffChunk>().Where(x => x.FourCC == "00dc");
            var enumerator = frames.GetEnumerator();
            //var timer = new System.Timers.Timer(1000 / Fps) { SynchronizingObject = this, AutoReset = true };
           
            while (enumerator.MoveNext())
            {
                var chunk = enumerator.Current;
                var frame = chunk.ReadToEnd();
                var image = decoder.Decode(frame, frame.Length);
                if (image == null) return;
                
            } 
        }
    }
}
