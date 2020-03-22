

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
        public long Uid { get; set; }
        //消息发送/接收客户端
        public Cellnet.CellnetClient CellnetClient;

        public ObservableCollection<UserMessageItem> MessageUsers { get; set; }
        public ObservableCollection<ContactItem> Contacts { get; set; }


        Dictionary<long, List<UserMessage>> UserMessages { get; set; }
        //登录凭证
        public string Token { get; set; }

        public ChatWindow()
        {
            InitializeComponent();
            MessageUsers = new ObservableCollection<UserMessageItem>();
            msgListView.DataContext = MessageUsers;
            //UserMessages.Add(new UserMessageItem("下次一定", "http://localhost:8080/static/avatar/default.jpg", "这是消息"));

            Contacts = new ObservableCollection<ContactItem>();
            contactListView.DataContext = Contacts;
            //Contacts.Add(new ContactItem
            //{
            //    Nickname = "下次一定",
            //    Avatar = "http://localhost:8080/static/avatar/default.jpg",
            //});
            UserMessages = new Dictionary<long, List<UserMessage>>();
            Current = this;
        }

        async private void msgListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var userMsgItem = msgListView.SelectedItem as UserMessageItem;
            await GetUserMessage(userMsgItem.UserId);

            while (!UserMessages.ContainsKey(userMsgItem.UserId))
            {

            }
            var msgs = UserMessages[userMsgItem.UserId];

            var p = new MessagePage();
            if (msgs != null)
            {
                foreach (var item in msgs)
                {
                    p.MessageList.Add(item);
                }
            }

            p.nicknameTb.Text = userMsgItem.Nickname;
            p.Avatar = userMsgItem.Avatar;
            p.Init();
            msgFrame.Navigate(p);
        }

        private Task GetUserMessage(long uid)
        {
            var req = new Protocol.GetMessageReq()
            {
                Header = new Protocol.ReqHeader { Token = Token },
                UserId = uid,
            };
            return CellnetClient.Send(req);
        }

        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {
            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.GetMessageUserAck).FullName)
            {
                //获取消息用户列表
                var msg = Protocol.GetMessageUserAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code != 200 || msg.Users == null)
                {
                    return;
                }
                foreach (var user in msg.Users)
                {
                    MessageUsers.Add(new UserMessageItem
                    {
                        UserId = user.UserId,
                        Avatar = user.Avatar,
                        Message = user.Messages.FirstOrDefault()?.Text,
                        Nickname = user.Nickname,
                    });
                }
            }
            else if (t.FullName == typeof(Protocol.GetMessageAck).FullName)
            {
                //获取用户消息
                var msg = Protocol.GetMessageAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Messages != null)
                {
                    var msgs = new List<UserMessage>(msg.Messages.Count);

                    var avatar = "";
                    foreach (var item in MessageUsers)
                    {
                        if (item.UserId == msg.UserId)
                        {
                            avatar = item.Avatar;
                            break;
                        }
                    }
                    foreach (var item in msg.Messages)
                    {
                        msgs.Add(new UserMessage
                        {
                            Avatar = avatar,
                            AvatarCol = item.ToUid == msg.UserId ? 1 : 0,
                            CreateTime = Util.TsToTime(item.CreateTime).ToString(),
                            Horizon = item.ToUid == msg.UserId ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                            MessageCol = item.ToUid == msg.UserId ? 0 : 1,
                            Text = item.Text,
                        });
                    }
                    if (!UserMessages.ContainsKey(msg.UserId))
                    {
                        UserMessages.Add(msg.UserId, msgs);
                    }
                    else
                    {
                        UserMessages[msg.UserId].AddRange(msgs);
                    }
                }

            }
            else if (t.FullName == typeof(Protocol.AddContactAck).FullName)
            {
                //添加联系人回复
                var msg = Protocol.AddContactAck.Parser.ParseFrom(args.Buffer);
            }
            else if (t.FullName == typeof(Protocol.MessageNotify).FullName)
            {
                //消息推送
                var msg = Protocol.MessageNotify.Parser.ParseFrom(args.Buffer);
            }
            else if (t.FullName == typeof(Protocol.RealTimeNotify).FullName)
            {
                //实时通信推送
                var msg = Protocol.RealTimeNotify.Parser.ParseFrom(args.Buffer);
            }
            else if (t.FullName == typeof(Protocol.RealTimeAck).FullName)
            {
                //实时通信回复
                var msg = Protocol.RealTimeAck.Parser.ParseFrom(args.Buffer);
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
    }
}
