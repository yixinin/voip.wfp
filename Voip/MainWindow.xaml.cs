

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
using Voip.G729;
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
        public G729Decoder AudioDecodder;

        //public VideoStreamDecoder VideoDecoder;

        //public Queue<VideoH264Packet> videoQueue;

        public MainWindow()
        {
            InitializeComponent();



            var videoQueue = new Queue<VideoH264Packet>(FPS * 10);
            VoipClient = new VoipClient(videoQueue, HOST, TCP_PORT, Id.Token, ROOM_ID);
            VoipClient.AudioBufferRecieved += VoipClient_AudioBufferRecieved;
            //VoipClient.VideoBufferRecieved += VoipClient_VideoBufferRecieved;
            AudioDecodder = new G729Decoder();
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

                }
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
            var buf = AudioDecodder.Process(e.Buffer);
            audioProvider.AddSamples(buf, 0, buf.Length);

            //Debug.WriteLine("recieved audio buffer", e.Buffer.Length);
        }
    }
}
