
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

        public ObservableCollection<Models.UserMessage> MessageList;

        public UserInfo Me;
        public UserInfo ToUser;

        public Utils.HttpClient httpClient;

        public MessagePage()
        {
            InitializeComponent();
            MessageList = new ObservableCollection<Models.UserMessage>();
            msgListView.DataContext = MessageList;
            Current = this;
        }






        async private void callVideoBtn_Click(object sender, RoutedEventArgs e)
        {

            var req = new Protocol.RealTimeReq
            {
                UserId = ToUser.Uid,
            };
            var ack = await httpClient.Send<Protocol.RealTimeReq, Protocol.RealTimeAck>(req);
            if (ack != null && ack.Header != null && ack.Header.Code == 200)
            {
                GotoVoipWindow(ack.RoomId, ack.Token, ack.TcpAddr);
            }
        }

        private void GotoVoipWindow(int rid, string token, string addr)
        {
            //创建voipwindow
            var voipWindow = new VoipWindow();
            voipWindow.nicknameText.Text = nicknameTb.Text;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(ToUser.Avatar);
            bmp.EndInit();
            voipWindow.avatarImg.ImageSource = bmp;

            var addrs = addr.Split(':');
            var host = addrs[0];
            var port = short.Parse(addrs[1]);
            voipWindow.InitVoip(rid, token, host, port);
            voipWindow.Show();
        }

    }

}
