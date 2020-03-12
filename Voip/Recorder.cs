using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class Recorder
    {
        public int HeaderSize { get; set; }
        public string Host { get; set; }
        public short Port { get; set; }
        private OpenCvSharp.VideoCapture _videoCapture;
        public OpenCvSharp.VideoCapture VideoCapture
        {
            get
            {
                return _videoCapture;
            }
        }

        private NAudio.Wave.WaveIn _audioCapture;
        public NAudio.Wave.WaveIn AudioCapture
        {
            get
            {
                return _audioCapture;
            }
        }

        private Socket _socketConn;


        const int TCP_BUFSIZE = 4096;

        public Recorder()
        {
            this._socketConn = new Socket(SocketType.Stream, ProtocolType.Tcp);
            this._videoCapture = new OpenCvSharp.VideoCapture(0);
            this._audioCapture = new NAudio.Wave.WaveIn();
            this._audioCapture.WaveFormat = new NAudio.Wave.WaveFormat(44100, 16, 2);
        }

        public void Connect()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(this.Host), this.Port);
            this._socketConn.Connect(endPoint);

        }

        private void Recv()
        {
            var preBuf = new byte[0];
            while (true)
            {
                var header = new byte[2 + 4];
                var n = this._socketConn.Receive(header);
                if (n != this.HeaderSize)
                {

                }

                var bodySize = Util.GetBufSize(header);

                var body = new byte[bodySize];
                var read = 0;
                while (read < bodySize)
                {
                    var needRead = 0;
                    var unread = bodySize - read;
                    if (unread > TCP_BUFSIZE)
                    {
                        needRead = TCP_BUFSIZE;
                    }
                    else
                    {
                        needRead = unread;
                    }

                    var subBody = new byte[needRead];
                    var n1 = this._socketConn.Receive(subBody);
                    for (var i = read; i < n1 + read; i++)
                    {
                        body[i] = subBody[i - read];
                    }
                    read += n1;
                }

                //处理包

            }

        }

        private void ReadVideoFrame()
        {
            while (true)
            {
                var mat = new OpenCvSharp.Mat();
                var ok = this._videoCapture.Read(mat);
                if (!ok)
                {
                    continue;
                }
            }

        }
    }
}
