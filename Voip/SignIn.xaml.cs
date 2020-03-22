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
using System.Windows.Shapes;

namespace Voip
{
    /// <summary>
    /// SignIn.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public Cellnet.CellnetClient cellnetClient;
        public string DefaultAvatar = "http://localhost:8080/static/avatar/default.jpg";
        public UserInfo UserInfo;

        private string TCP_HOST = "10.0.0.218";
        private int TCP_PORT = 8180;
        private bool isSignUp = false;
        public MainWindow()
        {
            InitializeComponent();
            Cellnet.Message.InitMessageIds();
            //读取设置
            UserInfo = new UserInfo();

            cellnetClient = new Cellnet.CellnetClient(TCP_HOST, TCP_PORT);

            cellnetClient.ConnectAsync().Wait();
            if (!cellnetClient.IsConnected)
            {
                MessageBox.Show("无法连接服务器");
                return;
            }
            cellnetClient.OnMessage += CellnetClient_OnMessage;
        }

        private void signUpBtn_Click(object sender, RoutedEventArgs e)
        {
            isSignUp = !isSignUp;
            if (isSignUp)
            {
                signUpBtn.Content = "直接登录";
                signInBtn.Content = "注册";
            }
            else
            {
                signUpBtn.Content = "注册账号";
                signInBtn.Content = "登录";
            }
        }

        async private void signInBtn_Click(object sender, RoutedEventArgs e)
        { 
            UserInfo.Password = uname.Text;
            if (isSignUp)
            {
                await cellnetClient.Send(new Protocol.SignUpReq
                {
                    Header = new Protocol.ReqHeader(),
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    DeviceCode = Util.GetDeviceCode(),
                    DeviceType = 2, 
                });
            }
            else
            {
                await cellnetClient.Send(new Protocol.SignInReq
                {
                    Username = uname.Text,
                    Password = UserInfo.Password,
                    //DeviceCode = Util.GetDeviceCode(),
                    DeviceType = 2,
                });
            }

            //GotoChat("");
            //this.Close();
        }

        public void CellnetClient_OnMessage(System.Net.Sockets.Socket sender, Cellnet.SocketMessageEventArgs args)
        {

            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.SignInAck).FullName)
            {
                var msg = Protocol.SignInAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Token != null && msg.Token != "")
                {
                    GotoChat(msg.Token);
                    return;
                }

            }
            else if (t.FullName == typeof(Protocol.SignUpAck).FullName)
            {
                var msg = Protocol.SignUpAck.Parser.ParseFrom(args.Buffer);
                if (msg.Header.Code == 200 && msg.Token != null && msg.Token != "")
                {
                    UserInfo.DeviceCode = msg.DeviceCode;
                    GotoChat(msg.Token);
                    return;
                }
            }

            var token = "asdasdas";

            //登录成功
            if (token != "")
            {
                //登录成功
                //记住密码  
                if (remberCheck.IsChecked.HasValue)
                {
                    UserInfo.Remember = remberCheck.IsChecked.Value ? "1" : "0";
                }
                if (autoSignCheck.IsChecked.HasValue)
                {
                    UserInfo.Remember = "1";
                    UserInfo.AutoSign = autoSignCheck.IsChecked.Value ? "1" : "0";
                }
                UserInfo.UpdateCache(uname.Text);


            }

        }

        public void GotoChat(string token)
        {

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                //跳转到主界面
                var chatPage = new ChatWindow();
                chatPage.Token = token;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                var avatar = UserInfo.Avatar;
                if (avatar == null || avatar == "")
                {
                    avatar = DefaultAvatar;
                }
                bmp.UriSource = new Uri(avatar);
                bmp.EndInit();
                chatPage.avatarImg.ImageSource = bmp;
                chatPage.CellnetClient = cellnetClient;
                chatPage.CellnetClient.OnMessage += chatPage.CellnetClient_OnMessage;

                chatPage.Show();

                cellnetClient.OnMessage -= CellnetClient_OnMessage;
                this.cellnetClient = null;
                this.Close();
            })); 
        }

    }



    public class UserInfo
    {
        //public string Username { get; set; }
        public string Password { get; set; }
        public string AutoSign { get; set; }
        public string Remember { get; set; }
        public string Avatar { get; set; }
        public string DeviceCode { get; set; }


        public UserInfo(string username = "")
        {
            if (username == "")
            {
                //读取默认用户
                username = Util.GetDefaultUserName();
            }
            //读取设置
            //this.Username = Util.GetSettingString(Util.Username,username);
            this.Password = Util.GetSettingString(Util.Password, username);
            this.AutoSign = Util.GetSettingString(Util.AutoSignIn, username);
            this.Remember = Util.GetSettingString(Util.Remember, username);
            this.Avatar = Util.GetSettingString(Util.Avatar, username);
            this.DeviceCode = Util.GetSettingString(Util.DeviceCode, username);
        }

        public void UpdateCache(string username)
        {
            Util.SetDefaultUsername(username);
            if (Remember == "1")
            {
                Util.UpdateSettingString(Util.Password, username, Password);
            }
            else
            {
                Util.UpdateSettingString(Util.Password, username, "");
            }
            Util.UpdateSettingString(Util.AutoSignIn, username, AutoSign);
            Util.UpdateSettingString(Util.Remember, username, Remember);
            Util.UpdateSettingString(Util.Avatar, username, Avatar);
            Util.UpdateSettingString(Util.DeviceCode, username, DeviceCode);
        }
    }
}
