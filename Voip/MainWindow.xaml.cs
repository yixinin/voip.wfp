
using FFmpeg.AutoGen;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
    public partial class MainWindow : Window
    {

        public static MainWindow Current { get; set; }

        const string HOST = "127.0.0.1";
        const short TCP_PORT = 9901;
        const string WsAddr = "ws://localhost:9902/live";
        const string PROTOCOL = "tcp";

        const string TOKEN = "00000000000000000000000000000000";
        const long ROOM_ID = 10240;

        public VoipClient VoipClient;


        public WaveOut audioPlayer;
        public BufferedWaveProvider audioProvider;

        public VideoStreamDecoder VideoDecoder;
        //public Stream audioStream;

        public MainWindow()
        {
            InitializeComponent();


            VoipClient = new VoipClient(HOST, TCP_PORT, TOKEN, ROOM_ID);
            VoipClient.AudioBufferRecieved += VoipClient_AudioBufferRecieved;
            VoipClient.VideoBufferRecieved += VoipClient_VideoBufferRecieved;
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
            Current = this;
            Task.Run(DecodeVideo);
        }

        public unsafe void DecodeVideo()
        {
            Util.ConfigureHWDecoder(out var HWDevice);
            VideoDecoder = new VideoStreamDecoder(HWDevice);
            {
                VideoDecoder.FrameSize = new System.Drawing.Size(320, 320);
                while (VideoDecoder.FrameSize.Width == 0)
                {
                    VideoDecoder.TryDecodeNextFrame(out var fm);
                }
                //获取到第一帧再开始解码
                var sourceSize = VideoDecoder.FrameSize;
                var sourcePixelFormat = HWDevice == AVHWDeviceType.AV_HWDEVICE_TYPE_NONE ? VideoDecoder.PixelFormat : Util.GetHWPixelFormat(HWDevice);
                var destinationSize = sourceSize;
                var destinationPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;
                using (var vfc = new VideoFrameConverter(sourceSize, sourcePixelFormat, destinationSize, destinationPixelFormat))
                {
                    var frameNumber = 0;
                    while (VideoDecoder.TryDecodeNextFrame(out var frame))
                    {
                        var convertedFrame = vfc.Convert(frame);

                        using (var bitmap = new Bitmap(convertedFrame.width, convertedFrame.height, convertedFrame.linesize[0], System.Drawing.Imaging.PixelFormat.Format24bppRgb, (IntPtr)convertedFrame.data[0]))
                        {
                            //bitmap.Save($"frame.{frameNumber:D8}.jpg", ImageFormat.Jpeg);
                            using (MemoryStream stream = new MemoryStream())
                            {
                                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); // 坑点：格式选Bmp时，不带透明度

                                stream.Position = 0;
                                BitmapImage bmp = new BitmapImage();
                                bmp.BeginInit();
                                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                                // Force the bitmap to load right now so we can dispose the stream.
                                bmp.CacheOption = BitmapCacheOption.OnLoad;
                                bmp.StreamSource = stream;
                                bmp.EndInit();
                                bmp.Freeze();
                                MessagePage.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                                {
                                    MessagePage.Current.videoImage.Source = bmp;

                                }));
                            }

                        }
                        frameNumber++;
                    }
                }
            }
        }



        private void VoipClient_VideoBufferRecieved(object sender, MediaBufferArgs e)
        {
            Debug.WriteLine("recieved video buffer", e.Buffer.Length);
            VideoDecoder.PutVideoStream(e.Buffer);
        }

        private void VoipClient_AudioBufferRecieved(object sender, MediaBufferArgs e)
        {

            audioProvider.AddSamples(e.Buffer, 0, e.Buffer.Length);

            //Debug.WriteLine("recieved audio buffer", e.Buffer.Length);
        }
    }
}
