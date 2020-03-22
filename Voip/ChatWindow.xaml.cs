

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

        //消息发送/接收客户端
        public Cellnet.CellnetClient CellnetClient; 

        public ObservableCollection<UserMessageItem> UserMessages { get; set; }

        //登录凭证
        public string Token { get; set; } 

        public ChatWindow()
        {
            InitializeComponent();
            UserMessages = new ObservableCollection<UserMessageItem>();
            msgListView.DataContext = UserMessages;
            UserMessages.Add(new UserMessageItem("下次一定", "http://localhost:8080/static/avatar/default.jpg", "这是消息"));

            Current = this;
        }

        private void msgListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var userMsgItem = msgListView.SelectedItem as UserMessageItem;
            var p = new MessagePage();
            p.nicknameTb.Text = userMsgItem.Nickname;
            p.Avatar = userMsgItem.Avatar;
            p.Init();
            msgFrame.Navigate(p);
        }

        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {

        }
    }
}
