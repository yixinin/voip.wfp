
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        const string HOST = "10.0.0.218";
        const short TCP_PORT = 9901;
        const string WsAddr = "ws://localhost:9902/live";
        const string PROTOCOL = "tcp";

        const string TOKEN = "00000000000000000000000000000000";
        const long ROOM_ID = 10240;

        public VoipClient VoipClient;


        public WaveOut audioPlayer;
        public BufferedWaveProvider audioProvider;
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
        }



        private void VoipClient_VideoBufferRecieved(object sender, MediaBufferArgs e)
        {
            Debug.WriteLine("recieved video buffer", e.Buffer.Length);
        }

        private void VoipClient_AudioBufferRecieved(object sender, MediaBufferArgs e)
        {
           
            audioProvider.AddSamples(e.Buffer, 0, e.Buffer.Length);
            
            //Debug.WriteLine("recieved audio buffer", e.Buffer.Length);
        }
    }
}
