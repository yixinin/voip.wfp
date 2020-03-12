
using System;
using System.Collections.Generic;
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
        public MainWindow()
        {
            InitializeComponent();
        }

        public void GetVideoCapture()
        {
            //OpenCvSharp4.Windows
            OpenCvSharp.VideoCapture cap = new OpenCvSharp.VideoCapture(0);
            cap.FrameWidth = 320;
            cap.FrameHeight = 320;
            cap.Fps = 24;
            cap.AutoFocus = true;
            while (true)
            {
                var frame = new OpenCvSharp.Mat();
                var hasFrame = cap.Read(frame);
                if (!hasFrame){
                    break;
                }
            }
        }


        public void GetAudioCapture()
        {
            var cap = new NAudio.Wave.WaveIn();
            cap.WaveFormat = new NAudio.Wave.WaveFormat(44100, 16, 1);
            cap.DataAvailable += Cap_DataAvailable;
        }

        private void Cap_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            var buf = e.Buffer;
        }
    }
}
