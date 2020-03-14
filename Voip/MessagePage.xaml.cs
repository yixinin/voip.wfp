
using System;
using System.Collections.Generic;
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

namespace Voip
{
    /// <summary>
    /// Interaction logic for MessagePage.xaml
    /// </summary>
    public partial class MessagePage : Page
    {
        public static MessagePage Current { get; set; }
        public MessagePage()
        {
            InitializeComponent();
            Current = this;
        }

        async private void callVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            var voipClient = MainWindow.Current.VoipClient;
            await voipClient.ConnectAsync("tcp");
            voipClient.CaptureAudio();
            voipClient.CaptureVideo();

            videoImage.Source = new BitmapImage();
        }

    }
}
