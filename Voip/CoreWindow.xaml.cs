

using NAudio.Wave;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public partial class ChatWindow : System.Windows.Window
    {

        public static ChatWindow Current { get; set; }
        public UserInfo UserInfo { get; set; }
        //消息发送/接收客户端
        public Cellnet.CellnetClient CellnetClient;
        public Utils.HttpClient httpClient;

        public ObservableCollection<Models.MessageUserItem> MessageUsers { get; set; }
        public ObservableCollection<Models.ContactItem> Contacts { get; set; }


        Dictionary<long, List<Models.UserMessage>> UserMessages { get; set; }
        //登录凭证
        public string Token { get; set; }

        public ChatWindow()
        {
            InitializeComponent();
            MessageUsers = new ObservableCollection<Models.MessageUserItem>();
            msgListView.DataContext = MessageUsers;
            //UserMessages.Add(new UserMessageItem("下次一定", "http://localhost:8080/static/avatar/default.jpg", "这是消息"));

            Contacts = new ObservableCollection<Models.ContactItem>();
            contactListView.DataContext = Contacts;
            //Contacts.Add(new ContactItem
            //{
            //    Nickname = "下次一定",
            //    Avatar = "http://localhost:8080/static/avatar/default.jpg",
            //});
            UserMessages = new Dictionary<long, List<Models.UserMessage>>();
            Current = this;
        }

        async private void msgListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var userMsgItem = msgListView.SelectedItem as Models.MessageUserItem;
            var msgs = await GetUserMessage(userMsgItem);
            if (msgs.Count > 0)
            {
                if (UserMessages.ContainsKey(userMsgItem.UserId))
                {
                    UserMessages[userMsgItem.UserId].AddRange(msgs);
                }
                else
                {
                    UserMessages.Add(userMsgItem.UserId, msgs);
                }
            }



            var p = new MessagePage();
            if (UserMessages.ContainsKey(userMsgItem.UserId))
            {
                foreach (var item in UserMessages[userMsgItem.UserId])
                {
                    p.MessageList.Add(item);
                }
            }

            p.nicknameTb.Text = userMsgItem.Nickname;
            p.Me = UserInfo;
            p.ToUser = new UserInfo
            {
                Avatar = userMsgItem.Avatar,
                Nickname = userMsgItem.Nickname,
                Uid = userMsgItem.UserId,
            };
            p.httpClient = new Utils.HttpClient(httpClient.URL);
            msgFrame.Navigate(p);
        }

        async private Task<List<Models.UserMessage>> GetUserMessage(Models.MessageUserItem u)
        {
            var list = new List<Models.UserMessage>();
            var req = new Protocol.GetMessageReq()
            {
                Header = new Protocol.ReqHeader { Token = Token },
                UserId = u.UserId,
            };

            var ack = await httpClient.Send<Protocol.GetMessageReq, Protocol.GetMessageAck>(req);
            if (ack != null && ack.Header != null && ack.Header.Code == 200)
            {
                foreach (var item in ack.Messages)
                {
                    var isMe = item.FromUid == UserInfo.Uid;
                    list.Add(new Models.UserMessage
                    {
                        AvatarCol = isMe ? 1 : 0,
                        MessageCol = isMe ? 0 : 1,
                        CreateTime = Utils.Util.TsToTime(item.CreateTime).ToString(),
                        Horizon = isMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        Text = item.Text,
                        Avatar = isMe ? UserInfo.Avatar : u.Avatar,
                    });
                }

            }
            return list;
        }

        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {
            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.MessageNotify).FullName)
            {
                //消息推送
                var msg = Protocol.MessageNotify.Parser.ParseFrom(args.Buffer);
            }
            else if (t.FullName == typeof(Protocol.RealTimeNotify).FullName)
            {
                //实时通信推送
                var msg = Protocol.RealTimeNotify.Parser.ParseFrom(args.Buffer);
                if (msg != null && msg.Header != null)
                {
                    if (msg.IsConnect)
                    {
                        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                        {

                            OpenVoipWindow(msg.RealTimeInfo.UserId, msg.RealTimeInfo.RoomId,
                                msg.RealTimeInfo.Token,
                                msg.RealTimeInfo.TcpAddr);
                        }));
                    }
                }
            }
        }

        private void contactListView_Selected(object sender, RoutedEventArgs e)
        {

        }

        private void contactListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void addContactBtn_Click(object sender, RoutedEventArgs e)
        {
            //添加联系人
            var req = new Protocol.AddContactReq
            {
                UserId = 1,
                SetRemarks = "me",
                Msg = "I request myself",
            };
            CellnetClient.Send(req);
        }

        private void OpenVoipWindow(long uid, int rid, string token, string addr)
        {

            var nickname = "";
            var avatar = "";
            foreach (var item in MessageUsers)
            {
                if (item.UserId == uid)
                {
                    nickname = item.Nickname;
                    avatar = item.Avatar;
                    break;
                }
            }
            //创建voipwindow
            var voipWindow = new VoipWindow();
            voipWindow.nicknameText.Text = nickname;
            if (avatar != "")
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(avatar);
                bmp.EndInit();
                voipWindow.avatarImg.ImageSource = bmp;
            }


            var addrs = addr.Split(':');
            if (addrs.Length == 2)
            {
                var host = addrs[0];
                var port = short.Parse(addrs[1]);
                voipWindow.InitVoip(rid, token, host, port);
                voipWindow.Show();
            }
            else
            {
                MessageBox.Show("发起通话失败");
            }
        }
    }
}
