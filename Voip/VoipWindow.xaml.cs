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
using System.Windows.Shapes;
using Voip.Av;
using Voip.G729;

namespace Voip
{
    /// <summary>
    /// VoipWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VoipWindow : Window
    {
        public string avatarUrl;
        public string nickname;

        const int FPS = 24;


        public Av.VoipClient VoipClient;
        public WaveOut audioPlayer;
        public BufferedWaveProvider audioProvider;
        public G729Decoder AudioDecodder;

        public static VoipWindow Current { get; internal set; }

        public VoipWindow()
        {
            InitializeComponent();
            Current = this;
        }

        public void InitVoip(int rid, string token, string host, short port)
        {
            var videoQueue = new Queue<Av.VideoH264Packet>(FPS * 10);
            VoipClient = new Av.VoipClient(videoQueue, host, port, token, rid);
            VoipClient.AudioBufferRecieved += VoipClient_AudioBufferRecieved;
            //VoipClient.VideoBufferRecieved += VoipClient_VideoBufferRecieved;
            AudioDecodder = new G729Decoder();
            PlayAudio();
            Task.Run(() =>
            {
                DecodeH264(videoQueue);
            });
            VoipClient.CaptureAudio();
            VoipClient.CaptureVideo();
        }

        private void hungBtn_Click(object sender, RoutedEventArgs e)
        {


            //结束通话
            //TODO 发送结束请求

            //断开tcp连接
            if (VoipClient != null && VoipClient.IsConnect)
            {
                VoipClient.StopCaptureAudio();
                VoipClient.StopCaptureVideo();
                VoipClient.Disconnect();
            }

            //关闭窗口
            this.Close();
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


        private void VoipClient_AudioBufferRecieved(object sender, MediaBufferArgs e)
        {
            var buf = AudioDecodder.Process(e.Buffer);
            audioProvider.AddSamples(buf, 0, buf.Length);

            //Debug.WriteLine("recieved audio buffer", e.Buffer.Length);
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
                if (videoQueue == null)
                {
                    return;
                }
                if (videoQueue.Count > 0)
                {
                    var p = videoQueue.Dequeue();
                    if (p == null)
                    {
                        continue;
                    }
                    var frames = Utils.Bytes.SplitH264Buffer(p.Buffer);
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
                            {
                                ShowBitmap(bmp, 0);
                                index++;
                            }

                        }
                    }

                }
            }

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

                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    videoImg.Source = bmp;
                }));
            }
        }

    }
}
