
using FFmpeg.AutoGen;
using NAudio.Wave;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using Windows.Media.Capture.Frames;
//using OpenCvSharp;

namespace Voip
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        public static MainWindow Current { get; set; }

        const string HOST = "10.0.0.218";
        const short TCP_PORT = 9901;
        const string WsAddr = "ws://localhost:9902/live";
        const string PROTOCOL = "tcp";

        const int FPS = 24;

        //const string TOKEN = "00000000000000000000000000000000";
        const long ROOM_ID = 10240;

        public VoipClient VoipClient;



        public WaveOut audioPlayer;
        public BufferedWaveProvider audioProvider;

        //public VideoStreamDecoder VideoDecoder;

        //public Queue<VideoH264Packet> videoQueue;

        public MainWindow()
        {
            InitializeComponent();



            var videoQueue = new Queue<VideoH264Packet>(FPS * 10);
            VoipClient = new VoipClient(videoQueue, HOST, TCP_PORT, Id.Token, ROOM_ID);
            VoipClient.AudioBufferRecieved += VoipClient_AudioBufferRecieved;
            //VoipClient.VideoBufferRecieved += VoipClient_VideoBufferRecieved;
            PlayAudio();
            Task.Run(() =>
            {
                DecodeH264(videoQueue);
            });


            //var ps = new string[44];
            //var path = @"C:\Users\yixin\Pictures\Uplay\";
            //for (var i = 1; i <= 44; i++)
            //{
            //    var fileName = $"frame.{i:D8}.jpg";
            //    ps[i - 1] = path + fileName;
            //}

            //H264Encode(ps, 30);

            //H264Decode(@"C:\Users\yixin\Desktop\test.avi", 30);


            Current = this;
        }

        public void PlayAudio()
        {
            try
            {
                audioPlayer = new WaveOut();
                var blockAlign = VoipClient.AudioChannels * (VoipClient.AudioBits / 8);
                int averageBytesPerSecond = VoipClient.AudioRate * blockAlign;
                var waveFormat = WaveFormat.CreateCustomFormat(
                    WaveFormatEncoding.Pcm,
                    VoipClient.AudioRate,
                    VoipClient.AudioChannels,
                    averageBytesPerSecond,
                    blockAlign,
                   VoipClient.AudioBits);
                audioProvider = new BufferedWaveProvider(waveFormat);
                audioProvider.DiscardOnBufferOverflow = true;
                audioPlayer.Init(audioProvider);
                audioPlayer.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("play audio ex:", ex);
            }
        }



        public unsafe void DecodeIOVideo(Queue<VideoH264Packet> videoQueue)
        {
            Util.ConfigureHWDecoder(out var HWDevice);

            using (var vsd = new IOVideoStreamDecoder(videoQueue, HWDevice))
            {
                Console.WriteLine($"codec name: {vsd.CodecName}");
                var info = vsd.GetContextInfo();
                info.ToList().ForEach(x => Console.WriteLine($"{x.Key} = {x.Value}"));

                var sourceSize = vsd.FrameSize;
                var sourcePixelFormat = HWDevice == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE ? vsd.PixelFormat : Util.GetHWPixelFormat(HWDevice);
                var destinationSize = sourceSize;
                var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
                using (var vfc = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat))
                {
                    var frameNumber = 0;
                    while (true)
                    {
                        if (!VoipClient.IsConnect)
                        {
                            continue;
                        }
                        vsd.TryDecodeNextFrame(out var frame);
                        var convertedFrame = vfc.Convert(frame);

                        using (var bitmap = new Bitmap(convertedFrame.width, convertedFrame.height, convertedFrame.linesize[0], System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)convertedFrame.data[0]))
                        {
                            ShowBitmap(bitmap, frameNumber);
                        }
                        frameNumber++;
                    }
                }
            }
        }

        public unsafe void DecodeVideo(Queue<VideoH264Packet> videoQueue)
        {

            //FFMediaToolkit.Decoding.VideoStream videoStream = new FFMediaToolkit.Decoding.MediaFile.Ope
            //var mf = FFMediaToolkit.Decoding.MediaFile.Open("");

            //ffmpeg.avcodec_register_all();

            //var vd = new VideoStreamDecoder(HWDevice)
            Util.ConfigureHWDecoder(out var HWDevice);
            Debug.WriteLine(string.Format("decode device {0}", HWDevice));
            using (var VideoDecoder = new VideoStreamDecoder(videoQueue, HWDevice))
            {
                VideoDecoder.FrameSize = new System.Drawing.Size(320, 320);


                var sourceSize = VideoDecoder.FrameSize;
                //var sourcePixelFormat = HWDevice == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE ? VideoDecoder.PixelFormat : Util.GetHWPixelFormat(HWDevice);
                var sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
                var destinationSize = sourceSize;
                var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
                using (var vfc = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat))
                {
                    var frameNumber = 0;

                    while (true)
                    {
                        if (!VoipClient.IsConnect)
                        {
                            continue;
                        }
                        if (VideoDecoder.TryDecodeNextFrame(out var frame))
                        {
                            //frame.format = 0;
                            var convertedFrame = vfc.Convert(frame);
                            //convertedFrame.channels = 4;

                            ConvertBitmap(convertedFrame, frameNumber);
                            frameNumber++;
                        }

                    }
                }
            }
        }

        private void H264Encode(string[] paths, int fps)
        {
            var path = paths[0];
            if (File.Exists(path))
            {
                Debug.WriteLine(path);
            }
            var firstFrame = new Bitmap(path);

            // AVIに出力するライターを作成(create AVI writer)
            var aviPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\test.avi";
            var aviFile = System.IO.File.OpenWrite(aviPath);
            var writer = new H264Writer(aviFile, firstFrame.Width, firstFrame.Height, fps);

            // H264エンコーダーを作成(create H264 encoder)
            var encoder = new OpenH264Lib.Encoder("openh264-2.0.0-win64.dll");

            // 1フレームエンコードするごとにライターに書き込み(write frame data for each frame encoded)
            OpenH264Lib.Encoder.OnEncodeCallback onEncode = (data, length, frameType) =>
            {
                var keyFrame = (frameType == OpenH264Lib.Encoder.FrameType.IDR) || (frameType == OpenH264Lib.Encoder.FrameType.I);
                writer.AddImage(data, keyFrame);
                Console.WriteLine("Encord {0} bytes, KeyFrame:{1}", length, keyFrame);
            };

            // H264エンコーダーの設定(encoder setup)
            int bps = 5000 * 1000;         // target bitrate. 5Mbps.
            float keyFrameInterval = 2.0f; // insert key frame interval. unit is second.
            encoder.Setup(firstFrame.Width, firstFrame.Height, bps, fps, keyFrameInterval, onEncode);

            // 1フレームごとにエンコード実施(do encode)
            for (int i = 0; i < paths.Length; i++)
            {
                var bmp = new Bitmap(paths[i]);
                encoder.Encode(bmp);
                bmp.Dispose();
            }

            writer.Close();
        }

        private void H264Decode(string path, int fps)
        {
            var decoder = new OpenH264Lib.Decoder("openh264-2.0.0-win64.dll");

            var aviFile = System.IO.File.OpenRead(path);
            var buf = new byte[aviFile.Length];

            var n = aviFile.Read(buf, 0, buf.Length);
            if (n != buf.Length)
            {
                Debug.WriteLine("not compelete");
            }
            var frames = Util.SplitH264Buffer(buf);
            foreach (var frame in frames)
            {
                var image = decoder.Decode(frame, frame.Length);
                if (image == null) return;
                ShowBitmap(image, 0);
            }
            //var riff = new RiffFile(aviFile);

            //var frames = riff.Chunks.OfType<RiffChunk>().Where(x => x.FourCC == "00dc");
            //var enumerator = frames.GetEnumerator();
            //var timer = new System.Timers.Timer(1000 / fps);
            //var index = 0;
            //timer.Elapsed += (s, e) =>
            //{
            //    if (enumerator.MoveNext() == false)
            //    {
            //        timer.Stop();
            //        return;
            //    }

            //    var chunk = enumerator.Current;
            //    var frame = chunk.ReadToEnd();
            //    Debug.WriteLine(Util.GetBufferText(frame));
            //    var image = decoder.Decode(frame, frame.Length);
            //    if (image == null) return;
            //    ShowBitmap(image, index);
            //    index++;
            //};
            //timer.Start();
        }

        public void DecodeH264(Queue<VideoH264Packet> videoQueue)
        {
            var decoder = new OpenH264Lib.Decoder("openh264-2.0.0-win64.dll");

            var index = 0;
            var hasKey = false;
            while (true)
            {
                if (!VoipClient.IsConnect)
                {
                    continue;
                }
                if (videoQueue.Count > 0)
                {
                    var p = videoQueue.Dequeue();
                    if (p == null)
                    {
                        continue;
                    }
                    var frames = Util.SplitH264Buffer(p.Buffer);
                    foreach (var frame in frames)
                    {
                        if (frame[4] == 103 || hasKey)
                        {
                            if (frame[4] == 103)
                            {
                                hasKey = true;
                            }
                            var bmp = decoder.Decode(frame, frame.Length);
                            if (bmp != null)
                                ShowBitmap(bmp, 0);
                        }


                    }
                    //var ms = new MemoryStream(p.Buffer.Length);
                    //{
                    //    ms.Write(p.Buffer, 0, p.Buffer.Length);
                    //    ms.Seek(0, SeekOrigin.Begin);

                    //    var riff = new RiffFile(ms); 

                    //    var frames = riff.Chunks.OfType<RiffChunk>().Where(x => x.FourCC == "00dc");
                    //    //ms.Close();
                    //    var enumerator = frames.GetEnumerator();
                    //    var timer = new System.Timers.Timer(1000 / 24);
                    //    timer.Elapsed += (s, e) =>
                    //    {
                    //        if (enumerator.MoveNext() == false)
                    //        {
                    //            timer.Stop();
                    //            return;
                    //        }

                    //        var chunk = enumerator.Current;
                    //        var frame = chunk.ReadToEnd();
                    //        var image = decoder.Decode(frame, frame.Length);
                    //        if (image == null) return;
                    //        ShowBitmap(image, 0);
                    //    };
                    //    timer.Start();
                    //}
                }
            }

        }
        private unsafe void ConverMat(AVFrame convertedFrame, int frameNumber)
        {
            var mat = new Mat((IntPtr)convertedFrame.data[0]);
            Bitmap bitmap = mat.ToBitmap();
            SaveToFile(bitmap, frameNumber);
        }
        private unsafe void ConvertBitmap(AVFrame convertedFrame, int frameNumber)
        {
            using (var bitmap = new Bitmap(convertedFrame.width,
                            convertedFrame.height,
                            convertedFrame.linesize[0],
                            System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                            (IntPtr)convertedFrame.data[0]))
            {
                ShowBitmap(bitmap, frameNumber);
                //SaveToFile(bitmap, frameNumber);

            }
        }

        private void SaveToFile(Bitmap bitmap, int frameNumber)
        {
            var path = $"C:\\Users\\yixin\\Pictures\\Uplay\\frame{frameNumber:D8}.jpg";
            bitmap.Save(path, ImageFormat.Jpeg);
            Debug.WriteLine(path);
        }


        private void ShowBitmap(Bitmap bitmap, int i)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png); // 坑点：格式选Bmp时，不带透明度



                stream.Position = 0;
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = stream;
                bmp.EndInit();
                bmp.Freeze();

                //Debug.WriteLine(i);

                MessagePage.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    MessagePage.Current.videoImage.Source = bmp;
                }));
            }
        }

        private void VoipClient_AudioBufferRecieved(object sender, MediaBufferArgs e)
        {

            audioProvider.AddSamples(e.Buffer, 0, e.Buffer.Length);

            //Debug.WriteLine("recieved audio buffer", e.Buffer.Length);
        }
    }
}
