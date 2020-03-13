using FFmpeg.AutoGen;
using NAudio.Wave;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voip
{

    public class MediaBufferArgs : EventArgs
    {
        public Int64 Uid { get; set; }
        public byte[] Buffer { get; set; }



        public MediaBufferArgs(byte[] buffer, long uid = 0)
        {
            this.Buffer = buffer;
            this.Uid = uid;
        }
    }
    public class VoipClient
    {

        //视频参数
        public int Width { get; set; }
        public int Height { get; set; }
        public int Fps { get; set; }
        public bool AutoFocus { get; set; }
        //音频参数
        public int AudioRate { get; set; }
        public int AudioBits { get; set; }
        public int AudioChannels { get; set; }


        public event EventHandler<MediaBufferArgs> VideoBufferRecieved;
        public event EventHandler<MediaBufferArgs> AudioBufferRecieved;

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

        //private Task videoTask;
        private CancellationTokenSource videoTokenSource;
        private bool _videoOn;
        public bool VideoOn { get { return _videoOn; } }

        //private Task socketTask;
        private CancellationTokenSource socketTokenSource;
        private bool _audioOn;
        public bool AudioOn { get { return _audioOn; } }

        public string Token { get; set; }
        public long RoomId { get; set; }

        const int TCP_BUFSIZE = 4096;
        const int HEADER_SIZE = 6;

        public Queue<VideoPacket> videoQueue;
        public Queue<AudioPacket> audioQueue;

        public VoipClient(string host, short port, string token, long roomId)
        {
            Host = host;
            Port = port;
            RoomId = roomId;
            Token = token;


            Fps = 24;
            Width = 320;
            Height = 320;
            AutoFocus = true;

            AudioBits = 16;
            AudioChannels = 1;
            AudioRate = 32000;
            videoQueue = new Queue<VideoPacket>();
            audioQueue = new Queue<AudioPacket>();
            FFmpegBinariesHelper.RegisterFFmpegBinaries();
        }

        public VoipClient()
        {
            Fps = 24;
            Width = 320;
            Height = 320;
            AutoFocus = true;

            AudioBits = 16;
            AudioChannels = 1;
            AudioRate = 8000;
            videoQueue = new Queue<VideoPacket>();
            audioQueue = new Queue<AudioPacket>();
            FFmpegBinariesHelper.RegisterFFmpegBinaries();
        }

        public void Connect(string p)
        {
            switch (p)
            {
                case "tcp":
                    ConnectTcp();
                    break;
                case "ws":
                    Debug.WriteLine("unsupport ws for now");
                    break;
                default:
                    Debug.WriteLine("unsupport protocol");
                    return;
            }
        }

        public async Task ConnectAsync(string p)
        {
            switch (p)
            {
                case "tcp":
                    await ConnectTcpAsync();
                    break;
                case "ws":
                    Debug.WriteLine("unsupport ws for now");
                    break;
                default:
                    Debug.WriteLine("unsupport protocol");
                    return;
            }
        }

        //加入聊天
        public void ConnectTcp()
        {
            if (Host == "" || Port == 0)
            {
                Debug.WriteLine("ConnectTcp no socket server available, host:port ?");
                return;
            }
            if (_socketConn == null)
            {
                this._socketConn = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
            var endPoint = new IPEndPoint(IPAddress.Parse(this.Host), this.Port);
            socketTokenSource = new CancellationTokenSource();


            var socketTask = new Task(() =>
            {
                Recv(socketTokenSource.Token);
            });
            _socketConn.ConnectAsync(endPoint).ContinueWith(t =>
            {
                if (_socketConn.Connected)
                {
                    socketTask.Start();
                    JoinLive();
                }
                else
                {
                    Debug.WriteLine("connect to " + Host + ":" + Port.ToString() + " fail");
                }

            });
        }

        public async Task ConnectTcpAsync()
        {
            if (Host == "" || Port == 0)
            {
                Debug.WriteLine("ConnectTcpAsync no socket server available, host:port ?");
                return;
            }
            if (_socketConn == null)
            {
                this._socketConn = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
            var endPoint = new IPEndPoint(IPAddress.Parse(this.Host), this.Port);
            Debug.WriteLine("connect to " + endPoint.ToString());
            await _socketConn.ConnectAsync(endPoint);

            if (_socketConn.Connected)
            {
                JoinLive();
                socketTokenSource = new CancellationTokenSource();
                Task.Run(() =>
                {
                    Recv(socketTokenSource.Token);
                });

            }
        }

        public void JoinLive()
        {
            var buf = Util.GetJoinBuffer(Token, RoomId);
            SendBuffer(buf);

        }

        //退出聊天
        public void Disconnect()
        {
            try
            {
                if (socketTokenSource != null && !socketTokenSource.IsCancellationRequested)
                {
                    socketTokenSource.Cancel();
                }

                _socketConn.Disconnect(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }

        //开启摄像头
        public void CaptureVideo()
        {
            if (!_socketConn.Connected)
            {
                Debug.WriteLine("CaptureVideo no socket connection available");
                return;
            }

            try
            {
                this._videoCapture = new OpenCvSharp.VideoCapture(0);
                _videoCapture.Fps = Fps;
                _videoCapture.FrameWidth = Width;
                _videoCapture.FrameHeight = Height;
                _videoCapture.AutoFocus = AutoFocus;

                videoTokenSource = new CancellationTokenSource();
                var ct = videoTokenSource.Token;
                _videoOn = true;
                Task.Run(handleVideoQueue);
                Task.Run(() =>
                {
                    ReadVideoFrame(ct);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("open camera ex:", ex);
            }
        }

        private void ReadVideoFrame(CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    //线程是否被取消，取消线程会抛出异常来终止线程
                    ct.ThrowIfCancellationRequested();

                    var mat = new OpenCvSharp.Mat();

                    var ok = this._videoCapture.Read(mat);
                    if (!ok)
                    {
                        Debug.WriteLine("no camera device available");
                        _videoOn = false;
                        _videoCapture.Dispose();
                        return;
                    }

                    videoQueue.Enqueue(new VideoPacket(mat.ToBytes()));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    _videoOn = false;
                    _videoCapture = null;
                    return;
                }

            }

        }

        //关闭摄像头
        public void StopCaptureVideo()
        {
            if (videoTokenSource != null && !videoTokenSource.IsCancellationRequested)
            {
                try
                {
                    videoTokenSource.Cancel();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            _videoOn = false;
            _videoCapture.Dispose();
        }

        //开启麦克风
        public void CaptureAudio()
        {
            if (!_socketConn.Connected)
            {
                Debug.WriteLine("CaptureAudio no socket connection available");
                return;
            }
            try
            {
                this._audioCapture = new WaveIn();
                //var waveFormat = new WaveFormat(AudioRate, AudioBits, AudioChannels);
                var blockAlign = AudioChannels * (AudioBits / 8);
                int averageBytesPerSecond = AudioRate * blockAlign;

                var waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Pcm,
                    AudioRate,
                    AudioChannels,
                    averageBytesPerSecond,
                    blockAlign,
                    AudioBits);

                this._audioCapture.WaveFormat = waveFormat;
                _audioCapture.DataAvailable += _audioCapture_DataAvailable;
                _audioCapture.RecordingStopped += _audioCapture_RecordingStopped;

                _audioCapture.StartRecording();
                _audioOn = true;
                Task.Run(handleAudioQueue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("open microphone ex:", ex);
            }
        }

        private void _audioCapture_DataAvailable(object sender, WaveInEventArgs e)
        {
            audioQueue.Enqueue(new AudioPacket(e.Buffer));
        }

        private void _audioCapture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Debug.WriteLine("audio recoding stoped", e);
            }
            _audioOn = false;
        }

        //关闭麦克风
        public void StopCaptureAudio()
        {
            _audioCapture.StopRecording();
            _audioOn = false;
            _audioCapture.Dispose();
        }


        private void handleAudioQueue()
        {
            while (AudioOn)
            {
                var total = 10;
                var bodySize = 0;
                var ps = new AudioPacket[total];
                for (var i = 0; i < total; i++)
                {
                    while (audioQueue.Count <= 0)
                    {
                        Task.Delay(1).ContinueWith(_ =>
                        {

                        });
                    }
                    var p = audioQueue.Dequeue();
                    ps[i] = p;
                    bodySize += p.Data.Length;
                }


                //每秒发送一个包
                var body = new byte[bodySize];
                var writed = 0;
                foreach (var p in ps)
                {
                    Array.Copy(p.Data, 0, body, writed, p.Data.Length);
                    writed += p.Data.Length;
                }
                var buf = Util.GetAudioBuffer(body);
                var n = _socketConn.Send(buf);
                if (n != buf.Length)
                {
                    Debug.WriteLine(string.Format("error: audio buf send, n={0}, bodySize={1}", n, bodySize));
                }
                //Debug.WriteLine(string.Format("audio samples:{0}, size:{1}", ps.Length, buf.Length));
            }
        }

        private void handleVideoQueue()
        {
            while (VideoOn)
            {
                var total = Fps;
                var ps = new VideoPacket[total];
                for (var i = 0; i < total; i++)
                {
                    while (videoQueue.Count <= 0)
                    {
                        Task.Delay(1).ContinueWith(_ =>
                        {

                        });
                    }
                    var p = videoQueue.Dequeue();
                    ps[i] = p;
                }

                //转码
                using (var fs = new MemoryStream()) //存储转码后的buffer
                {
                    EncodeVideo(ps, fs);

                    var body = new byte[fs.Length];
                    fs.Seek(0, SeekOrigin.Begin);
                    var read = 0;

                    while (read < fs.Length)
                    {
                        var sub = new byte[fs.Length - read];
                        var msn = fs.Read(sub, 0, sub.Length);
                        Array.Copy(sub, 0, body, read, msn);
                        read += msn;
                    }
                    var buf = Util.GetVideoBuffer(body);
                    var n = _socketConn.Send(buf);
                    if (n != buf.Length)
                    {
                        Debug.WriteLine(string.Format("error: video buf send, n={0}, bodySize={1}", n, buf.Length));
                    }
                    Debug.WriteLine(string.Format("video frames:{0}, size:{1}", ps.Length, buf.Length));
                }

            }
        }

        private unsafe void EncodeVideo(VideoPacket[] ps, MemoryStream fs)
        {
            var sourceSize = new System.Drawing.Size(Height, Width);
            var sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
            var destinationSize = sourceSize;
            var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
            using (var vfc = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat))
            {
                using (var vse = new H264VideoStreamEncoder(fs, Fps, destinationSize))
                {
                    var frameNumber = 0;
                    //读取
                    foreach (var p in ps)
                    {
                        byte[] bitmapData;
                        using (var ms = new MemoryStream(p.Data))
                        {
                            using (var frameImage = Image.FromStream(ms))
                            {
                                using (var frameBitmap = frameImage is Bitmap bitmap ? bitmap : new Bitmap(frameImage))
                                {
                                    bitmapData = Util.GetBitmapData(frameBitmap);
                                }
                            }
                        }


                        fixed (byte* pBitmapData = bitmapData)
                        {
                            var data = new byte_ptrArray8 { [0] = pBitmapData };
                            var linesize = new int_array8 { [0] = bitmapData.Length / sourceSize.Height };
                            var frame = new AVFrame
                            {
                                data = data,
                                linesize = linesize,
                                height = sourceSize.Height,
                                width = sourceSize.Width,
                            };
                            var convertedFrame = vfc.Convert(frame);
                            convertedFrame.pts = frameNumber * Fps;
                            vse.Encode(convertedFrame);
                        }

                        //Console.WriteLine($"frame: {frameNumber}");
                        frameNumber++;

                    }
                }
            }
        }

        private void Recv(CancellationToken ct)
        {
            while (true)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var header = new byte[HEADER_SIZE];
                    var n = this._socketConn.Receive(header);
                    while (n < HEADER_SIZE)
                    {
                        var need = HEADER_SIZE - n;
                        var sub = new byte[need];
                        var n1 = _socketConn.Receive(sub);
                        Array.Copy(sub, 0, header, n, n1);
                        n += n1;
                    }

                    Debug.WriteLine(Util.GetBufferText(header));
                    if (header[0] != 2 || header[1] > 2)
                    {
                        Debug.WriteLine("header error");
                        continue;
                    }

                    var bodySize = Util.GetBufSize(header);
                    if (bodySize == 0)
                    {
                        Debug.WriteLine("body size is 0", header);
                        continue;
                    }

                    var body = new byte[bodySize];
                    var read = 0;
                    while (read < bodySize)
                    {
                        var unread = bodySize - read;

                        var needRead = unread > TCP_BUFSIZE ? TCP_BUFSIZE : unread;


                        var subBody = new byte[needRead];
                        var n1 = this._socketConn.Receive(subBody);
                        //Debug.WriteLine(string.Format("n={0},n1={1},sublen={2},bodylen={3},size={4}", n, n1, subBody.Length, body.Length, bodySize));
                        Array.Copy(subBody, 0, body, read, n1);
                        read += n1;
                    }

                    //处理包
                    switch (header[1])
                    {
                        case 1:
                            //audio
                            AudioBufferRecieved?.Invoke(this, new MediaBufferArgs(body));
                            break;
                        case 2:
                            //video
                            VideoBufferRecieved?.Invoke(this, new MediaBufferArgs(body));
                            break;
                        default:
                            Debug.WriteLine("unknown buf header", header);
                            break;

                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return;
                }
            }
        }

        private void SendBuffer(byte[] buf)
        {
            if (buf == null || buf.Length < HEADER_SIZE)
            {
                Debug.WriteLine("buf is too small");
                return;
            }
            if (_socketConn != null && _socketConn.Connected)
            {
                var n = _socketConn.Send(buf);
                if (n != buf.Length)
                {
                    //没有发送完
                    Debug.WriteLine(buf[1].ToString() + " buf not send compelete. " + n.ToString() + "<" + buf.Length.ToString());
                }
                return;
            }
            Debug.WriteLine("socket is not connected");
        }


    }
}
