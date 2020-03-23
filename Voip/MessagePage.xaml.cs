
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Voip.Cellnet;

namespace Voip
{
    /// <summary>
    /// Interaction logic for MessagePage.xaml
    /// </summary>
    public partial class MessagePage : Page
    {

        public string Token { get; set; }
        public static MessagePage Current { get; set; }

        public ObservableCollection<Models.UserMessage> MessageList { get; set; }

        public UserInfo Me;
        public UserInfo ToUser;

        public Utils.HttpClient httpClient;
        public Cellnet.CellnetClient CellnetClient { get; set; }

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
                Header = new Protocol.ReqHeader { Token = Token },
                UserId = ToUser.Uid,
            };
            var ack = await httpClient.Send<Protocol.RealTimeReq, Protocol.RealTimeAck>(req);
            if (ack != null && ack.Header != null && ack.Header.Code == 200)
            {
                GotoVoipWindow(ack.RoomId, ack.Token, ack.TcpAddr);
            }
            else
            {

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

        async private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            var req = new Protocol.SendMessageReq
            {
                Header = new Protocol.ReqHeader { Token = Token },
                Body = new Protocol.MessageBody
                {
                    MessageType = 1,
                    Text = msgTb.Text,
                    ToUserId = ToUser.Uid,
                },
            };
            var ack = await httpClient.Send<Protocol.SendMessageReq, Protocol.SendMessageAck>(req);
            if (ack.Header.Code != 200)
            {
                Debug.WriteLine(ack.Header.Msg);
            }
            msgTb.Text = "";
        }
        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {
            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.MessageNotify).FullName)
            {


                //消息推送
                var msg = Protocol.MessageNotify.Parser.ParseFrom(args.Buffer);
                if (msg.FromUserId == ToUser.Uid || msg.Body.ToUserId == ToUser.Uid)
                {
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    {
                        var isMe = msg.FromUserId == Me.Uid;
                        MessageList.Add(new Models.UserMessage
                        {
                            AvatarCol = isMe ? 1 : 0,
                            MessageCol = isMe ? 0 : 1,
                            CreateTime = DateTime.Now.ToString(),
                            Horizon = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                            Text = msg.Body.Text,
                            Avatar = isMe ? Config.HttpAddr + msg.Avatar : Me?.Avatar,
                        });
                        msgListView.SelectedIndex = msgListView.Items.Count - 1;
                        msgListView.ScrollIntoView(msgListView.SelectedItem);
                    }));
                }

            } 
        }

    }

}
