
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Voip.Cellnet;

namespace Voip
{
    /// <summary>
    /// Interaction logic for MessagePage.xaml
    /// </summary>
    public partial class MessagePage : Page
    {
        public static MessagePage Current { get; set; }

        public ObservableCollection<UserMessage> MessageList;

        public string Avatar { get; set; }



        public MessagePage()
        {
            InitializeComponent();
            MessageList = new ObservableCollection<UserMessage>();
            msgListView.DataContext = MessageList;
            Current = this;
        }

        public void Init()
        {

            //var bmp = new BitmapImage();
            //bmp.BeginInit();
            //bmp.UriSource = new Uri(Avatar);
            //bmp.EndInit();
            //var msg1 = new UserMessage
            //{
            //    Text = "lkjhgfdsa",
            //    CreateTime = DateTime.Now.ToString(),
            //    Horizon = HorizontalAlignment.Right,
            //    Avatar = bmp,
            //    AvatarCol = 1,
            //    MessageCol = 0,
            //};
            //var msg2 = new UserMessage
            //{
            //    Text = "asdfghjkl",
            //    CreateTime = DateTime.Now.ToString(),
            //    Horizon = HorizontalAlignment.Left,
            //    Avatar = bmp,
            //    AvatarCol = 0,
            //    MessageCol = 1,
            //};
            //MessageList.Add(msg1);
            //MessageList.Add(msg2);
        }




        async private void callVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            //var voipClient = ChatWindow.Current.VoipClient;
            //await voipClient.ConnectAsync("tcp");
            //voipClient.CaptureAudio();
            //voipClient.CaptureVideo();

            //videoImage.Source = new BitmapImage();

            var req = new Protocol.RealTimeReq
            {
                 ContactId = 1,
            };
            ChatWindow.Current.CellnetClient.Send(req);
        }

        private void GotoVideoWindow()
        {
            //创建voipwindow
            var voipWindow = new VoipWindow();
            voipWindow.nicknameText.Text = nicknameTb.Text;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(Avatar);
            bmp.EndInit();
            voipWindow.avatarImg.ImageSource = bmp;
            voipWindow.Show();
        }

    }

    public class UserMessage
    {
        public string Text { get; set; }
        public string Avatar { get; set; }
        public string CreateTime { get; set; }

        public HorizontalAlignment Horizon { get; set; }

        public int AvatarCol { get; set; }
        public int MessageCol { get; set; }

    }
}
